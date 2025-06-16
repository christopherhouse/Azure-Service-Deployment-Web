using Azure.Core;
using Microsoft.Identity.Web;
using System.Text.Json;

namespace AzureDeploymentWeb.Services
{
    public interface IAzureDeploymentService
    {
        Task<DeploymentResult> DeployTemplateAsync(string templateContent, string parametersContent, string deploymentName);
        Task<string> GetDeploymentStatusAsync(string deploymentName);
    }

    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string? DeploymentName { get; set; }
        public string? ResourceGroupName { get; set; }
        public object? Outputs { get; set; }
        public string? Error { get; set; }
    }

    public class AzureDeploymentService : IAzureDeploymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;

        public AzureDeploymentService(IConfiguration configuration, ITokenAcquisition tokenAcquisition)
        {
            _configuration = configuration;
            _tokenAcquisition = tokenAcquisition;
            _subscriptionId = _configuration["Azure:SubscriptionId"] ?? 
                throw new InvalidOperationException("Azure:SubscriptionId not configured");
            _resourceGroupName = _configuration["Azure:ResourceGroup"] ?? 
                throw new InvalidOperationException("Azure:ResourceGroup not configured");
        }

        public async Task<DeploymentResult> DeployTemplateAsync(string templateContent, string parametersContent, string deploymentName)
        {
            try
            {
                // Get access token for Azure Resource Manager
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                    new[] { "https://management.azure.com/user_impersonation" });

                // Validate template and parameters
                var template = JsonDocument.Parse(templateContent);
                var parameters = ExtractParameters(parametersContent);
                
                // For demo purposes, simulate a successful deployment
                // In a real implementation, you would use Azure.ResourceManager to deploy the template
                await Task.Delay(2000); // Simulate deployment time

                return new DeploymentResult
                {
                    Success = true,
                    DeploymentName = deploymentName,
                    ResourceGroupName = _resourceGroupName,
                    Outputs = new { message = "Deployment completed successfully (demo)" }
                };
            }
            catch (Exception ex)
            {
                return new DeploymentResult
                {
                    Success = false,
                    DeploymentName = deploymentName,
                    ResourceGroupName = _resourceGroupName,
                    Error = ex.Message
                };
            }
        }

        public async Task<string> GetDeploymentStatusAsync(string deploymentName)
        {
            try
            {
                // Get access token for Azure Resource Manager
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                    new[] { "https://management.azure.com/user_impersonation" });

                // For demo purposes, return succeeded status
                // In a real implementation, you would query the actual deployment status
                await Task.Delay(100);
                return "Succeeded";
            }
            catch
            {
                return "Failed";
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