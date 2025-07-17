namespace AzureDeploymentWeb.Models
{
    public class ServiceBusOptions
    {
        public const string SectionName = "ServiceBus";

        public string ConnectionString { get; set; } = string.Empty;
        public string TopicName { get; set; } = "deployments";
        public string SubscriptionName { get; set; } = "all-messages";
    }
}