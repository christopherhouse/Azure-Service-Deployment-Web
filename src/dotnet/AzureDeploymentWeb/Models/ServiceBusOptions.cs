namespace AzureDeploymentWeb.Models
{
    public class ServiceBusOptions
    {
        public const string SectionName = "ServiceBus";

        public string NamespaceEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string TopicName { get; set; } = "deployments";
        public string SubscriptionName { get; set; } = "all-messages";
    }
}