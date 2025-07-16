using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDeploymentWeb.Services;

namespace AzureDeploymentWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureResourcesController : ControllerBase
    {
        private readonly IAzureResourceDiscoveryService _discoveryService;
        private readonly IConfiguration _configuration;

        public AzureResourcesController(IAzureResourceDiscoveryService discoveryService, IConfiguration configuration)
        {
            _discoveryService = discoveryService;
            _configuration = configuration;
        }

        private bool IsAuthenticationConfigured()
        {
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
        }

        private IActionResult? CheckAuthorizationIfConfigured()
        {
            if (IsAuthenticationConfigured() && !User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }
            return null;
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            var authResult = CheckAuthorizationIfConfigured();
            if (authResult != null) return authResult;

            try
            {
                var subscriptions = await _discoveryService.GetSubscriptionsAsync();
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("resourcegroups/{subscriptionId}")]
        public async Task<IActionResult> GetResourceGroups(string subscriptionId)
        {
            var authResult = CheckAuthorizationIfConfigured();
            if (authResult != null) return authResult;

            try
            {
                if (string.IsNullOrEmpty(subscriptionId))
                {
                    return BadRequest(new { error = "Subscription ID is required" });
                }

                var resourceGroups = await _discoveryService.GetResourceGroupsAsync(subscriptionId);
                return Ok(resourceGroups);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}