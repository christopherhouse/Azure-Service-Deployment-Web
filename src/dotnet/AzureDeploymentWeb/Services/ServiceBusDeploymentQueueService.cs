using Azure.Messaging.ServiceBus;
using AzureDeploymentWeb.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Collections.Concurrent;

namespace AzureDeploymentWeb.Services
{
    public class ServiceBusDeploymentQueueService : IServiceBusDeploymentQueueService, IAsyncDisposable
    {
        private readonly ServiceBusOptions _options;
        private readonly ILogger<ServiceBusDeploymentQueueService> _logger;
        private readonly ServiceBusClient? _client;
        private readonly ServiceBusSender? _sender;
        private readonly ServiceBusProcessor? _processor;
        private readonly ConcurrentQueue<DeploymentJob> _localQueue = new();
        private readonly SemaphoreSlim _messageSemaphore = new(1, 1);
        private readonly IServiceProvider _serviceProvider;

        public bool IsEnabled { get; private set; }

        public ServiceBusDeploymentQueueService(
            IOptions<ServiceBusOptions> options,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusDeploymentQueueService> logger)
        {
            _options = options.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;

            if (!string.IsNullOrEmpty(_options.ConnectionString))
            {
                try
                {
                    _client = new ServiceBusClient(_options.ConnectionString);
                    _sender = _client.CreateSender(_options.TopicName);
                    
                    var processorOptions = new ServiceBusProcessorOptions
                    {
                        MaxConcurrentCalls = 1,
                        AutoCompleteMessages = false,
                        ReceiveMode = ServiceBusReceiveMode.PeekLock
                    };
                    
                    _processor = _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, processorOptions);
                    _processor.ProcessMessageAsync += ProcessMessageAsync;
                    _processor.ProcessErrorAsync += ProcessErrorAsync;
                    
                    IsEnabled = true;
                    _logger.LogInformation("Service Bus deployment queue service initialized with topic '{TopicName}' and subscription '{SubscriptionName}'", 
                        _options.TopicName, _options.SubscriptionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Service Bus deployment queue service. Falling back to in-memory queue.");
                    IsEnabled = false;
                }
            }
            else
            {
                _logger.LogInformation("Service Bus connection string not provided. Using in-memory queue fallback.");
                IsEnabled = false;
            }
        }

        public void EnqueueJob(DeploymentJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (IsEnabled && _sender != null)
            {
                _ = Task.Run(async () => await EnqueueJobAsync(job));
            }
            else
            {
                // Fallback to in-memory queue
                _localQueue.Enqueue(job);
                _logger.LogInformation("Enqueued deployment job {JobId} for deployment {DeploymentName} by user {UserName} (in-memory fallback)", 
                    job.JobId, job.DeploymentName, job.UserName);
            }
        }

        private async Task EnqueueJobAsync(DeploymentJob job)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(job);
                var message = new ServiceBusMessage(messageBody)
                {
                    MessageId = job.JobId.ToString(),
                    Subject = job.DeploymentName,
                    ApplicationProperties =
                    {
                        ["UserName"] = job.UserName,
                        ["DeploymentName"] = job.DeploymentName,
                        ["SubscriptionId"] = job.SubscriptionId,
                        ["ResourceGroupName"] = job.ResourceGroupName
                    }
                };

                await _sender!.SendMessageAsync(message);
                _logger.LogInformation("Sent deployment job {JobId} for deployment {DeploymentName} by user {UserName} to Service Bus", 
                    job.JobId, job.DeploymentName, job.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send deployment job {JobId} to Service Bus. Adding to local queue as fallback.", job.JobId);
                _localQueue.Enqueue(job);
            }
        }

        public bool TryDequeueJob(out DeploymentJob? job)
        {
            // For Service Bus, messages are processed via the processor, so we only check the local fallback queue
            var result = _localQueue.TryDequeue(out job);
            if (result && job != null)
            {
                _logger.LogInformation("Dequeued deployment job {JobId} for deployment {DeploymentName} (fallback queue)", 
                    job.JobId, job.DeploymentName);
            }
            return result;
        }

        public int GetQueueCount()
        {
            // Return local queue count (Service Bus queue depth is not easily accessible)
            return _localQueue.Count;
        }

        public IEnumerable<DeploymentJob> GetPendingJobs()
        {
            // Return local queue jobs (Service Bus pending messages are not easily enumerable)
            return _localQueue.ToArray();
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (IsEnabled && _processor != null)
            {
                await _processor.StartProcessingAsync(cancellationToken);
                _logger.LogInformation("Started Service Bus message processing");
            }
        }

        public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (IsEnabled && _processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                _logger.LogInformation("Stopped Service Bus message processing");
            }
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var messageBody = args.Message.Body.ToString();
                var job = JsonSerializer.Deserialize<DeploymentJob>(messageBody);
                
                if (job != null)
                {
                    _logger.LogInformation("Received deployment job {JobId} for deployment {DeploymentName} from Service Bus", 
                        job.JobId, job.DeploymentName);

                    // Process the deployment job
                    await ProcessDeploymentJobAsync(job);

                    // Complete the message to remove it from the queue
                    await args.CompleteMessageAsync(args.Message);
                    _logger.LogInformation("Completed processing of deployment job {JobId}", job.JobId);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize deployment job from Service Bus message");
                    await args.DeadLetterMessageAsync(args.Message, "DESERIALIZATION_ERROR", "Failed to deserialize DeploymentJob");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Service Bus message. Message will be retried.");
                // Don't complete the message - it will be retried automatically
                // After max retry attempts, it will go to dead letter queue
            }
        }

        private async Task ProcessDeploymentJobAsync(DeploymentJob job)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var deploymentService = scope.ServiceProvider.GetRequiredService<IAzureDeploymentService>();
                
                // Start the deployment using the existing service
                var result = await deploymentService.StartAsyncDeploymentAsync(
                    job.TemplateContent,
                    job.ParametersContent,
                    job.DeploymentName,
                    job.SubscriptionId,
                    job.ResourceGroupName);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully started deployment {DeploymentName} for job {JobId}", 
                        job.DeploymentName, job.JobId);

                    // Start tracking the deployment using existing monitoring service
                    var monitoringService = scope.ServiceProvider.GetServices<IHostedService>()
                        .OfType<DeploymentMonitoringService>()
                        .FirstOrDefault();

                    if (monitoringService != null)
                    {
                        await monitoringService.TrackDeployment(
                            job.DeploymentName, 
                            job.UserName, 
                            job.StartTime, 
                            job.SubscriptionId, 
                            job.ResourceGroupName);
                        
                        _logger.LogInformation("Started tracking deployment {DeploymentName} for user {UserName}", 
                            job.DeploymentName, job.UserName);
                    }
                }
                else
                {
                    _logger.LogError("Failed to start deployment {DeploymentName} for job {JobId}: {Error}", 
                        job.DeploymentName, job.JobId, result.Error);
                    throw new InvalidOperationException($"Deployment failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deployment job {JobId} for deployment {DeploymentName}", 
                    job.JobId, job.DeploymentName);
                throw; // Re-throw to trigger Service Bus retry logic
            }
        }

        private async Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Service Bus error in processor for source '{Source}': {Error}", 
                args.ErrorSource, args.Exception.Message);
            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_processor != null)
            {
                await _processor.DisposeAsync();
            }
            
            if (_sender != null)
            {
                await _sender.DisposeAsync();
            }
            
            if (_client != null)
            {
                await _client.DisposeAsync();
            }
            
            _messageSemaphore.Dispose();
        }
    }
}