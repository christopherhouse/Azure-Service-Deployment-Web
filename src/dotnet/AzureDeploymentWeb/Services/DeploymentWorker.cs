using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public class DeploymentWorker : BackgroundService
    {
        private readonly IDeploymentQueueService _queueService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeploymentWorker> _logger;

        public DeploymentWorker(
            IDeploymentQueueService queueService,
            IServiceProvider serviceProvider,
            ILogger<DeploymentWorker> logger)
        {
            _queueService = queueService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DeploymentWorker background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_queueService.TryDequeueJob(out var job) && job != null)
                    {
                        await ProcessDeploymentJob(job);
                    }
                    else
                    {
                        // No jobs in queue, wait before checking again
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DeploymentWorker background service");
                    // Wait longer on error to avoid rapid retries
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("DeploymentWorker background service stopped");
        }

        private async Task ProcessDeploymentJob(DeploymentJob job)
        {
            _logger.LogInformation("Processing deployment job {JobId} for deployment {DeploymentName}", 
                job.JobId, job.DeploymentName);

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
                    else
                    {
                        _logger.LogWarning("DeploymentMonitoringService not found, deployment {DeploymentName} will not be tracked", 
                            job.DeploymentName);
                    }
                }
                else
                {
                    _logger.LogError("Failed to start deployment {DeploymentName} for job {JobId}: {Error}", 
                        job.DeploymentName, job.JobId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deployment job {JobId} for deployment {DeploymentName}", 
                    job.JobId, job.DeploymentName);
            }
        }
    }
}