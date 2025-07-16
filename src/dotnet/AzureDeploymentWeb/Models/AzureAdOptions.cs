namespace AzureDeploymentWeb.Models
{
    public class AzureAdOptions
    {
        public const string SectionName = "AzureAd";
        public string Instance { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
    }
}
