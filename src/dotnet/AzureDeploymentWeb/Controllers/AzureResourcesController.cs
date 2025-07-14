using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDeploymentWeb.Services;

namespace AzureDeploymentWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AzureResourcesController : ControllerBase
    {
        private readonly IAzureResourceDiscoveryService _discoveryService;

        public AzureResourcesController(IAzureResourceDiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
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