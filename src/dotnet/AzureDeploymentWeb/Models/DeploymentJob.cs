namespace AzureDeploymentWeb.Models
{
    public class DeploymentJob
    {
        public Guid JobId { get; set; } = Guid.NewGuid();
        public string TemplateContent { get; set; } = string.Empty;
        public string ParametersContent { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string ResourceGroupName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }
}