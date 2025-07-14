using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly CacheOptions _cacheOptions;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            IDistributedCache cache, 
            IOptions<CacheOptions> cacheOptions,
            ILogger<CacheController> logger)
        {
            _cache = cache;
            _cacheOptions = cacheOptions.Value;
            _logger = logger;
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearCache([FromBody] ClearCacheRequest request)
        {
            try
            {
                var keysCleared = new List<string>();

                if (request.ClearSubscriptions)
                {
                    await _cache.RemoveAsync("azure_subscriptions");
                    keysCleared.Add("subscriptions");
                    _logger.LogInformation("Cleared subscriptions cache");
                }

                if (request.ClearResourceGroups && !string.IsNullOrEmpty(request.SubscriptionId))
                {
                    var key = $"azure_resource_groups_{request.SubscriptionId}";
                    await _cache.RemoveAsync(key);
                    keysCleared.Add($"resource_groups_{request.SubscriptionId}");
                    _logger.LogInformation("Cleared resource groups cache for subscription {SubscriptionId}", request.SubscriptionId);
                }
                else if (request.ClearResourceGroups)
                {
                    // Note: In a production environment, you might want to maintain a list of cache keys
                    // or use a cache implementation that supports pattern-based deletion
                    _logger.LogWarning("Cannot clear all resource group caches without subscription ID");
                    return BadRequest(new { error = "SubscriptionId is required when clearing resource group caches" });
                }

                return Ok(new { message = "Cache cleared successfully", keysCleared });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cache");
                return StatusCode(500, new { error = "Failed to clear cache" });
            }
        }

        [HttpGet("info")]
        public IActionResult GetCacheInfo()
        {
            return Ok(new
            {
                provider = _cacheOptions.Provider,
                subscriptionsCacheDuration = _cacheOptions.SubscriptionsCacheDurationMinutes,
                resourceGroupsCacheDuration = _cacheOptions.ResourceGroupsCacheDurationMinutes,
                redisConfigured = !string.IsNullOrEmpty(_cacheOptions.Redis.ConnectionString)
            });
        }
    }

    public class ClearCacheRequest
    {
        public bool ClearSubscriptions { get; set; }
        public bool ClearResourceGroups { get; set; }
        public string? SubscriptionId { get; set; }
    }
}