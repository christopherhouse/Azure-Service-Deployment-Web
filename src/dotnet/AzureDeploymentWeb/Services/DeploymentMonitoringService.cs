using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using AzureDeploymentWeb.Hubs;
using AzureDeploymentWeb.Services;
using AzureDeploymentWeb.Models;
using System.Text.Json;

namespace AzureDeploymentWeb.Services
{
    public class DeploymentMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<DeploymentHub> _hubContext;
        private readonly IDistributedCache _cache;
        private readonly CacheOptions _cacheOptions;
        private readonly ILogger<DeploymentMonitoringService> _logger;
        
        private const string ActiveDeploymentsListKey = "active_deployments_list";
        private const string DeploymentTrackerKeyPrefix = "deployment_tracker_";

        public DeploymentMonitoringService(
            IServiceProvider serviceProvider,
            IHubContext<DeploymentHub> hubContext,
            IDistributedCache cache,
            IOptions<CacheOptions> cacheOptions,
            ILogger<DeploymentMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _cache = cache;
            _cacheOptions = cacheOptions.Value;
            _logger = logger;
        }

        public async Task TrackDeployment(string deploymentName, string userName, DateTime startTime, string subscriptionId, string resourceGroupName)
        {
            try
            {
                var tracker = new DeploymentTracker
                {
                    DeploymentName = deploymentName,
                    UserName = userName,
                    StartTime = startTime,
                    LastChecked = DateTime.UtcNow,
                    SubscriptionId = subscriptionId,
                    ResourceGroupName = resourceGroupName
                };

                // Store the deployment tracker
                var trackerKey = $"{DeploymentTrackerKeyPrefix}{deploymentName}";
                var trackerJson = JsonSerializer.Serialize(tracker);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.DeploymentTrackingCacheDurationMinutes)
                };
                
                await _cache.SetStringAsync(trackerKey, trackerJson, cacheOptions);

                // Add to active deployments list
                await AddToActiveDeploymentsList(deploymentName);
                
                _logger.LogInformation("Started tracking deployment {DeploymentName} for user {UserName} in subscription {SubscriptionId}, resource group {ResourceGroupName}", 
                    deploymentName, userName, subscriptionId, resourceGroupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track deployment {DeploymentName} in cache", deploymentName);
                throw;
            }
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
            var activeDeployments = await GetActiveDeploymentsList();

            foreach (var deploymentName in activeDeployments)
            {
                try
                {
                    var tracker = await GetDeploymentTracker(deploymentName);
                    if (tracker == null)
                    {
                        // Tracker not found, mark for removal from list
                        deploymentsToRemove.Add(deploymentName);
                        continue;
                    }

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
                            
                            // Update tracker in cache
                            await UpdateDeploymentTracker(tracker);
                            
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
                        else
                        {
                            // Update last checked time
                            tracker.LastChecked = DateTime.UtcNow;
                            await UpdateDeploymentTracker(tracker);
                        }
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
                    var tracker = await GetDeploymentTracker(deploymentName);
                    if (tracker != null && DateTime.UtcNow.Subtract(tracker.StartTime) > TimeSpan.FromHours(2))
                    {
                        deploymentsToRemove.Add(deploymentName);
                        _logger.LogWarning("Removing deployment {DeploymentName} after 2 hours of monitoring", deploymentName);
                    }
                }
            }

            // Remove completed or failed deployments from tracking
            foreach (var deploymentName in deploymentsToRemove)
            {
                await RemoveFromTracking(deploymentName);
            }
        }

        private async Task<List<string>> GetActiveDeploymentsList()
        {
            try
            {
                var listJson = await _cache.GetStringAsync(ActiveDeploymentsListKey);
                if (!string.IsNullOrEmpty(listJson))
                {
                    var list = JsonSerializer.Deserialize<List<string>>(listJson);
                    return list ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve active deployments list from cache");
            }
            
            return new List<string>();
        }

        private async Task AddToActiveDeploymentsList(string deploymentName)
        {
            try
            {
                var activeDeployments = await GetActiveDeploymentsList();
                if (!activeDeployments.Contains(deploymentName))
                {
                    activeDeployments.Add(deploymentName);
                    var listJson = JsonSerializer.Serialize(activeDeployments);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.DeploymentTrackingCacheDurationMinutes)
                    };
                    await _cache.SetStringAsync(ActiveDeploymentsListKey, listJson, cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add deployment {DeploymentName} to active deployments list", deploymentName);
            }
        }

        private async Task<DeploymentTracker?> GetDeploymentTracker(string deploymentName)
        {
            try
            {
                var trackerKey = $"{DeploymentTrackerKeyPrefix}{deploymentName}";
                var trackerJson = await _cache.GetStringAsync(trackerKey);
                if (!string.IsNullOrEmpty(trackerJson))
                {
                    return JsonSerializer.Deserialize<DeploymentTracker>(trackerJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve deployment tracker for {DeploymentName} from cache", deploymentName);
            }
            
            return null;
        }

        private async Task UpdateDeploymentTracker(DeploymentTracker tracker)
        {
            try
            {
                var trackerKey = $"{DeploymentTrackerKeyPrefix}{tracker.DeploymentName}";
                var trackerJson = JsonSerializer.Serialize(tracker);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.DeploymentTrackingCacheDurationMinutes)
                };
                await _cache.SetStringAsync(trackerKey, trackerJson, cacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update deployment tracker for {DeploymentName} in cache", tracker.DeploymentName);
            }
        }

        private async Task RemoveFromTracking(string deploymentName)
        {
            try
            {
                // Remove from active deployments list
                var activeDeployments = await GetActiveDeploymentsList();
                if (activeDeployments.Remove(deploymentName))
                {
                    var listJson = JsonSerializer.Serialize(activeDeployments);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.DeploymentTrackingCacheDurationMinutes)
                    };
                    await _cache.SetStringAsync(ActiveDeploymentsListKey, listJson, cacheOptions);
                }

                // Remove deployment tracker
                var trackerKey = $"{DeploymentTrackerKeyPrefix}{deploymentName}";
                await _cache.RemoveAsync(trackerKey);
                
                _logger.LogInformation("Removed deployment {DeploymentName} from tracking", deploymentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove deployment {DeploymentName} from tracking", deploymentName);
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