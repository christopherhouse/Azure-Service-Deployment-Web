namespace AzureDeploymentWeb.Models
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";
        
        public RedisOptions Redis { get; set; } = new();
        public int SubscriptionsCacheDurationMinutes { get; set; } = 60;
        public int ResourceGroupsCacheDurationMinutes { get; set; } = 30;
    }

    public class RedisOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
}