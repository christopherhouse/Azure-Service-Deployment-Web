using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public class ServiceBusDeploymentWorker : BackgroundService
    {
        private readonly IServiceBusDeploymentQueueService _queueService;
        private readonly ILogger<ServiceBusDeploymentWorker> _logger;

        public ServiceBusDeploymentWorker(
            IServiceBusDeploymentQueueService queueService,
            ILogger<ServiceBusDeploymentWorker> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ServiceBusDeploymentWorker background service started");

            if (_queueService.IsEnabled)
            {
                _logger.LogInformation("Service Bus is enabled, starting message processing");
                await _queueService.StartProcessingAsync(stoppingToken);
                
                // Keep the service running while processing messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            else
            {
                _logger.LogInformation("Service Bus is not enabled, processing local queue fallback");
                // Process local queue fallback similar to original DeploymentWorker
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_queueService.TryDequeueJob(out var job) && job != null)
                        {
                            // Note: Service Bus queue service handles the actual processing
                            // This is just for fallback scenarios
                            _logger.LogInformation("Processing fallback queue job {JobId}", job.JobId);
                        }
                        else
                        {
                            // No jobs in fallback queue, wait before checking again
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ServiceBusDeploymentWorker background service");
                        // Wait longer on error to avoid rapid retries
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                }
            }

            _logger.LogInformation("ServiceBusDeploymentWorker background service stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ServiceBusDeploymentWorker stopping...");
            
            if (_queueService.IsEnabled)
            {
                await _queueService.StopProcessingAsync(cancellationToken);
            }
            
            await base.StopAsync(cancellationToken);
        }
    }
}