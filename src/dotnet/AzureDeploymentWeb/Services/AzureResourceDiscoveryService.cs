using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

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

        public AzureResourceDiscoveryService()
        {
            // Use DefaultAzureCredential for authentication
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
        }

        public async Task<List<SubscriptionInfo>> GetSubscriptionsAsync()
        {
            try
            {
                var subscriptions = new List<SubscriptionInfo>();
                
                await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
                {
                    subscriptions.Add(new SubscriptionInfo
                    {
                        SubscriptionId = subscription.Data.SubscriptionId ?? string.Empty,
                        DisplayName = subscription.Data.DisplayName ?? subscription.Data.SubscriptionId ?? "Unknown"
                    });
                }

                return subscriptions;
            }
            catch (Exception)
            {
                // Return empty list if there's an error (e.g., no access to subscriptions)
                return new List<SubscriptionInfo>();
            }
        }

        public async Task<List<ResourceGroupInfo>> GetResourceGroupsAsync(string subscriptionId)
        {
            try
            {
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

                return resourceGroups;
            }
            catch (Exception)
            {
                // Return empty list if there's an error (e.g., no access to resource groups)
                return new List<ResourceGroupInfo>();
            }
        }
    }
}