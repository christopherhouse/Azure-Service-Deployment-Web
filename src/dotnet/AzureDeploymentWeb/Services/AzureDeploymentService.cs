using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text.Json;
using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public interface IAzureDeploymentService
    {
        Task<DeploymentResult> DeployTemplateAsync(string templateContent, string parametersContent, string deploymentName, string subscriptionId, string resourceGroupName);
        Task<string> GetDeploymentStatusAsync(string deploymentName, string subscriptionId, string resourceGroupName);
        Task<DeploymentResult> StartAsyncDeploymentAsync(string templateContent, string parametersContent, string deploymentName, string subscriptionId, string resourceGroupName);
        Task<DeploymentNotification?> GetDeploymentDetailsAsync(string deploymentName, string subscriptionId, string resourceGroupName);
    }

    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string? DeploymentName { get; set; }
        public string? ResourceGroupName { get; set; }
        public object? Outputs { get; set; }
        public string? Error { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class AzureDeploymentService : IAzureDeploymentService
    {
        private readonly ArmClient _armClient;

        public AzureDeploymentService()
        {
            // Use DefaultAzureCredential for authentication
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
        }

        public async Task<DeploymentResult> DeployTemplateAsync(string templateContent, string parametersContent, string deploymentName, string subscriptionId, string resourceGroupName)
        {
            try
            {
                // Get subscription and resource group
                var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                var resourceGroups = subscription.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                // Parse template JSON
                var template = JsonDocument.Parse(templateContent);
                
                // Parse parameters - handle both ARM parameter file format and direct parameters
                var parametersObject = ExtractParameters(parametersContent);
                
                // Create deployment properties
                var deploymentProperties = new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                {
                    Template = BinaryData.FromString(templateContent),
                    Parameters = BinaryData.FromString(JsonSerializer.Serialize(parametersObject))
                };

                // Create deployment content
                var deploymentContent = new ArmDeploymentContent(deploymentProperties);

                // Start the deployment
                var deployments = resourceGroup.Value.GetArmDeployments();
                var deploymentOperation = await deployments.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, 
                    deploymentName, 
                    deploymentContent);

                var deployment = deploymentOperation.Value;
                var deploymentData = deployment.Data;

                return new DeploymentResult
                {
                    Success = deploymentData.Properties.ProvisioningState == ResourcesProvisioningState.Succeeded,
                    DeploymentName = deploymentName,
                    ResourceGroupName = resourceGroupName,
                    Outputs = deploymentData.Properties.Outputs?.ToObjectFromJson<object>(),
                    Error = deploymentData.Properties.ProvisioningState != ResourcesProvisioningState.Succeeded 
                        ? $"Deployment failed with state: {deploymentData.Properties.ProvisioningState}" 
                        : null,
                    StartTime = deploymentData.Properties.Timestamp?.DateTime ?? DateTime.UtcNow,
                    EndTime = deploymentData.Properties.ProvisioningState == ResourcesProvisioningState.Succeeded ||
                             deploymentData.Properties.ProvisioningState == ResourcesProvisioningState.Failed 
                             ? DateTime.UtcNow : null
                };
            }
            catch (Exception ex)
            {
                return new DeploymentResult
                {
                    Success = false,
                    DeploymentName = deploymentName,
                    ResourceGroupName = resourceGroupName,
                    Error = ex.Message,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                };
            }
        }

        public async Task<DeploymentResult> StartAsyncDeploymentAsync(string templateContent, string parametersContent, string deploymentName, string subscriptionId, string resourceGroupName)
        {
            try
            {
                // Get subscription and resource group
                var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                var resourceGroups = subscription.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                // Parse template JSON
                var template = JsonDocument.Parse(templateContent);
                
                // Parse parameters - handle both ARM parameter file format and direct parameters
                var parametersObject = ExtractParameters(parametersContent);
                
                // Create deployment properties
                var deploymentProperties = new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                {
                    Template = BinaryData.FromString(templateContent),
                    Parameters = BinaryData.FromString(JsonSerializer.Serialize(parametersObject))
                };

                // Create deployment content
                var deploymentContent = new ArmDeploymentContent(deploymentProperties);

                // Start the deployment WITHOUT waiting for completion
                var deployments = resourceGroup.Value.GetArmDeployments();
                var deploymentOperation = await deployments.CreateOrUpdateAsync(
                    Azure.WaitUntil.Started,  // Only wait for the deployment to start
                    deploymentName, 
                    deploymentContent);

                return new DeploymentResult
                {
                    Success = true,
                    DeploymentName = deploymentName,
                    ResourceGroupName = resourceGroupName,
                    StartTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new DeploymentResult
                {
                    Success = false,
                    DeploymentName = deploymentName,
                    ResourceGroupName = resourceGroupName,
                    Error = ex.Message,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                };
            }
        }

        public async Task<string> GetDeploymentStatusAsync(string deploymentName, string subscriptionId, string resourceGroupName)
        {
            try
            {
                // Get subscription and resource group
                var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                var resourceGroups = subscription.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                // Get the deployment
                var deployments = resourceGroup.Value.GetArmDeployments();
                var deployment = await deployments.GetAsync(deploymentName);
                
                // Return the provisioning state as string
                return deployment.Value.Data.Properties.ProvisioningState?.ToString() ?? "Unknown";
            }
            catch (Exception)
            {
                // If deployment not found or any other error, return Failed
                return "Failed";
            }
        }

        public async Task<DeploymentNotification?> GetDeploymentDetailsAsync(string deploymentName, string subscriptionId, string resourceGroupName)
        {
            try
            {
                // Get subscription and resource group
                var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                var resourceGroups = subscription.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                // Get the deployment
                var deployments = resourceGroup.Value.GetArmDeployments();
                var deployment = await deployments.GetAsync(deploymentName);
                var deploymentData = deployment.Value.Data;
                
                var notification = new DeploymentNotification
                {
                    DeploymentName = deploymentName,
                    Status = deploymentData.Properties.ProvisioningState?.ToString() ?? "Unknown",
                    StartTime = deploymentData.Properties.Timestamp?.DateTime ?? DateTime.UtcNow,
                    ResourceGroup = resourceGroupName
                };

                // Set end time if deployment is completed
                if (notification.IsCompleted)
                {
                    notification.EndTime = DateTime.UtcNow; // ARM doesn't provide exact end time, so we use current time
                }

                return notification;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private object ExtractParameters(string parametersContent)
        {
            var parametersDoc = JsonDocument.Parse(parametersContent);
            
            // If the parameters object has a 'parameters' property, it's a full ARM parameter file
            if (parametersDoc.RootElement.TryGetProperty("parameters", out var parametersElement))
            {
                return JsonSerializer.Deserialize<object>(parametersElement.GetRawText()) ?? new object();
            }
            
            // Otherwise, assume it's already in the correct format
            return JsonSerializer.Deserialize<object>(parametersContent) ?? new object();
        }
    }
}