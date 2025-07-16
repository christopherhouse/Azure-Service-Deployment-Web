using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public interface IAzureResourceDiscoveryService
    {
        Task<List<SubscriptionInfo>> GetSubscriptionsAsync();
        Task<List<ResourceGroupInfo>> GetResourceGroupsAsync(string subscriptionId);
    }

    public class SubscriptionInfo
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class ResourceGroupInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class AzureResourceDiscoveryService : IAzureResourceDiscoveryService
    {
        private readonly ArmClient _armClient;
        private readonly IDistributedCache _cache;
        private readonly CacheOptions _cacheOptions;
        private readonly ILogger<AzureResourceDiscoveryService> _logger;

        private const string SubscriptionsCacheKey = "azure_subscriptions";
        private const string ResourceGroupsCacheKeyPrefix = "azure_resource_groups_";

        public AzureResourceDiscoveryService(
            IDistributedCache cache, 
            IOptions<CacheOptions> cacheOptions,
            ILogger<AzureResourceDiscoveryService> logger)
        {
            // Detect if running locally
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            DefaultAzureCredential credential;
            if (isLocal)
            {
                _logger.LogInformation("Running locally - using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }
            else
            {
                // Use user assigned managed identity client id if provided
                var uamiClientId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

                if (string.IsNullOrEmpty(uamiClientId)
                {
                    uamiClientId = "1bc37b1a-d4a7-4f5c-bdc5-18a3142e73fa";
                }

                _logger.LogInformation($"Running with user assigned MI client ID: {uamiClientId}");
                if (!string.IsNullOrEmpty(uamiClientId))
                {
                    credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = uamiClientId
                    });
                }
                else
                {
                    credential = new DefaultAzureCredential();
                }
            }
            _armClient = new ArmClient(credential);
            _cache = cache;
            _cacheOptions = cacheOptions.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<SubscriptionInfo>> GetSubscriptionsAsync()
        {
            // Try to get from cache first
            try
            {
                var cachedData = await _cache.GetStringAsync(SubscriptionsCacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Retrieved subscriptions from cache");
                    var cachedSubscriptions = JsonSerializer.Deserialize<List<SubscriptionInfo>>(cachedData);
                    if (cachedSubscriptions != null)
                    {
                        // Ensure cached data is also sorted
                        return cachedSubscriptions.OrderBy(s => s.DisplayName).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve subscriptions from cache");
            }

            // Cache miss or error - fetch from Azure
            try
            {
                _logger.LogInformation("Fetching subscriptions from Azure API");
                var subscriptions = new List<SubscriptionInfo>();
                
                await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                {
                    subscriptions.Add(new SubscriptionInfo
                    {
                        SubscriptionId = subscription.Data.SubscriptionId ?? string.Empty,
                        DisplayName = subscription.Data.DisplayName ?? subscription.Data.SubscriptionId ?? "Unknown"
                    });
                }

                // Sort subscriptions alphabetically by display name
                var sortedSubscriptions = subscriptions.OrderBy(s => s.DisplayName).ToList();

                // Cache the result
                try
                {
                    var cacheData = JsonSerializer.Serialize(sortedSubscriptions);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.SubscriptionsCacheDurationMinutes)
                    };
                    await _cache.SetStringAsync(SubscriptionsCacheKey, cacheData, cacheOptions);
                    _logger.LogInformation("Cached {Count} subscriptions for {Duration} minutes", 
                        sortedSubscriptions.Count, _cacheOptions.SubscriptionsCacheDurationMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache subscriptions");
                }

                if (sortedSubscriptions.Count == 0)
                {
                    _logger.LogWarning("No subscriptions found or accessible");
                }
                else
                {
                    _logger.LogInformation("Found {Count} subscriptions", sortedSubscriptions.Count);
                }

                return sortedSubscriptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch subscriptions from Azure API");
                // Return empty list if there's an error (e.g., no access to subscriptions)
                return new List<SubscriptionInfo>();
            }
        }

        public async Task<List<ResourceGroupInfo>> GetResourceGroupsAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                return new List<ResourceGroupInfo>();
            }

            var cacheKey = $"{ResourceGroupsCacheKeyPrefix}{subscriptionId}";

            // Try to get from cache first
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Retrieved resource groups for subscription {SubscriptionId} from cache", subscriptionId);
                    var cachedResourceGroups = JsonSerializer.Deserialize<List<ResourceGroupInfo>>(cachedData);
                    if (cachedResourceGroups != null)
                    {
                        // Ensure cached data is also sorted
                        return cachedResourceGroups.OrderBy(rg => rg.Name).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve resource groups from cache for subscription {SubscriptionId}", subscriptionId);
            }

            // Cache miss or error - fetch from Azure
            try
            {
                _logger.LogInformation("Fetching resource groups for subscription {SubscriptionId} from Azure API", subscriptionId);
                var resourceGroups = new List<ResourceGroupInfo>();
                
                var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                
                await foreach (var resourceGroup in subscription.GetResourceGroups().GetAllAsync())
                {
                    resourceGroups.Add(new ResourceGroupInfo
                    {
                        Name = resourceGroup.Data.Name,
                        Location = resourceGroup.Data.Location.Name
                    });
                }

                // Sort resource groups alphabetically by name
                var sortedResourceGroups = resourceGroups.OrderBy(rg => rg.Name).ToList();

                // Cache the result
                try
                {
                    var cacheData = JsonSerializer.Serialize(sortedResourceGroups);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.ResourceGroupsCacheDurationMinutes)
                    };
                    await _cache.SetStringAsync(cacheKey, cacheData, cacheOptions);
                    _logger.LogInformation("Cached {Count} resource groups for subscription {SubscriptionId} for {Duration} minutes", 
                        sortedResourceGroups.Count, subscriptionId, _cacheOptions.ResourceGroupsCacheDurationMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache resource groups for subscription {SubscriptionId}", subscriptionId);
                }

                if (sortedResourceGroups.Count == 0)
                {
                    _logger.LogWarning("No resource groups found or accessible for subscription {SubscriptionId}", subscriptionId);
                }
                else
                {
                    _logger.LogInformation("Found {Count} resource groups for subscription {SubscriptionId}", 
                        sortedResourceGroups.Count, subscriptionId);
                }

                return sortedResourceGroups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch resource groups from Azure API for subscription {SubscriptionId}", subscriptionId);
                // Return empty list if there's an error (e.g., no access to resource groups)
                return new List<ResourceGroupInfo>();
            }
        }
    }
}