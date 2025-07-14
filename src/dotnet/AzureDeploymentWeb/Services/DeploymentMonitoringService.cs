using Microsoft.AspNetCore.SignalR;
using AzureDeploymentWeb.Hubs;
using AzureDeploymentWeb.Services;
using AzureDeploymentWeb.Models;
using System.Collections.Concurrent;

namespace AzureDeploymentWeb.Services
{
    public class DeploymentMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<DeploymentHub> _hubContext;
        private readonly ILogger<DeploymentMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, DeploymentTracker> _activeDeployments = new();

        public DeploymentMonitoringService(
            IServiceProvider serviceProvider,
            IHubContext<DeploymentHub> hubContext,
            ILogger<DeploymentMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        public void TrackDeployment(string deploymentName, string userName, DateTime startTime, string subscriptionId, string resourceGroupName)
        {
            _activeDeployments.TryAdd(deploymentName, new DeploymentTracker
            {
                DeploymentName = deploymentName,
                UserName = userName,
                StartTime = startTime,
                LastChecked = DateTime.UtcNow,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName
            });
            
            _logger.LogInformation("Started tracking deployment {DeploymentName} for user {UserName} in subscription {SubscriptionId}, resource group {ResourceGroupName}", 
                deploymentName, userName, subscriptionId, resourceGroupName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckActiveDeployments();
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Check every 10 seconds
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in deployment monitoring service");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait longer on error
                }
            }
        }

        private async Task CheckActiveDeployments()
        {
            var deploymentsToRemove = new List<string>();

            foreach (var (deploymentName, tracker) in _activeDeployments)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var deploymentService = scope.ServiceProvider.GetRequiredService<IAzureDeploymentService>();
                    
                    var notification = await deploymentService.GetDeploymentDetailsAsync(deploymentName, tracker.SubscriptionId, tracker.ResourceGroupName);
                    
                    if (notification != null)
                    {
                        notification.StartTime = tracker.StartTime; // Use the actual start time we recorded
                        
                        // Check if status changed or if it's been a while since last update
                        bool shouldNotify = tracker.LastStatus != notification.Status || 
                                          DateTime.UtcNow.Subtract(tracker.LastNotification) > TimeSpan.FromMinutes(1);
                        
                        if (shouldNotify)
                        {
                            await _hubContext.Clients.Group($"user_{tracker.UserName}")
                                .SendAsync("DeploymentStatusUpdate", notification);
                            
                            tracker.LastStatus = notification.Status;
                            tracker.LastNotification = DateTime.UtcNow;
                            
                            _logger.LogInformation("Sent status update for deployment {DeploymentName}: {Status}", 
                                deploymentName, notification.Status);
                        }
                        
                        // If deployment is completed, mark for removal
                        if (notification.IsCompleted)
                        {
                            deploymentsToRemove.Add(deploymentName);
                            _logger.LogInformation("Deployment {DeploymentName} completed with status {Status}", 
                                deploymentName, notification.Status);
                        }
                        
                        tracker.LastChecked = DateTime.UtcNow;
                    }
                    else
                    {
                        // If we can't get deployment details, it might be failed or deleted
                        deploymentsToRemove.Add(deploymentName);
                        
                        var failedNotification = new DeploymentNotification
                        {
                            DeploymentName = deploymentName,
                            Status = "Failed",
                            StartTime = tracker.StartTime,
                            EndTime = DateTime.UtcNow,
                            Message = "Deployment not found or failed to retrieve status"
                        };
                        
                        await _hubContext.Clients.Group($"user_{tracker.UserName}")
                            .SendAsync("DeploymentStatusUpdate", failedNotification);
                        
                        _logger.LogWarning("Could not retrieve status for deployment {DeploymentName}, marking as failed", deploymentName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking status for deployment {DeploymentName}", deploymentName);
                    
                    // If deployment has been failing for too long, remove it
                    if (DateTime.UtcNow.Subtract(tracker.StartTime) > TimeSpan.FromHours(2))
                    {
                        deploymentsToRemove.Add(deploymentName);
                        _logger.LogWarning("Removing deployment {DeploymentName} after 2 hours of monitoring", deploymentName);
                    }
                }
            }

            // Remove completed or failed deployments from tracking
            foreach (var deploymentName in deploymentsToRemove)
            {
                _activeDeployments.TryRemove(deploymentName, out _);
            }
        }

        private class DeploymentTracker
        {
            public string DeploymentName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime LastChecked { get; set; }
            public DateTime LastNotification { get; set; }
            public string? LastStatus { get; set; }
            public string SubscriptionId { get; set; } = string.Empty;
            public string ResourceGroupName { get; set; } = string.Empty;
        }
    }
}