using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace AzureDeploymentWeb.Controllers
{
    public class DeploymentController : Controller
    {
        private readonly IAzureDeploymentService _deploymentService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DeploymentController(
            IAzureDeploymentService deploymentService, 
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _deploymentService = deploymentService;
            _serviceProvider = serviceProvider;
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
                return Challenge();
            }
            return null;
        }

        public IActionResult Index()
        {
            var authResult = CheckAuthorizationIfConfigured();
            if (authResult != null) return authResult;

            var model = new DeploymentViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deploy(DeploymentViewModel model)
        {
            var authResult = CheckAuthorizationIfConfigured();
            if (authResult != null) return authResult;

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // Read template file
                string templateContent;
                using (var reader = new StreamReader(model.TemplateFile!.OpenReadStream()))
                {
                    templateContent = await reader.ReadToEndAsync();
                }

                // Read parameters file
                string parametersContent;
                using (var reader = new StreamReader(model.ParametersFile!.OpenReadStream()))
                {
                    parametersContent = await reader.ReadToEndAsync();
                }

                // Generate deployment name
                var deploymentName = $"webapp-deployment-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                var startTime = DateTime.UtcNow;

                // Start async deployment with selected subscription and resource group
                var result = await _deploymentService.StartAsyncDeploymentAsync(
                    templateContent, 
                    parametersContent, 
                    deploymentName,
                    model.SelectedSubscriptionId!,
                    model.SelectedResourceGroupName!);

                if (result.Success)
                {
                    // Start tracking the deployment
                    var userName = User.Identity?.Name ?? "unknown";
                    var monitoringService = _serviceProvider.GetServices<IHostedService>()
                        .OfType<DeploymentMonitoringService>()
                        .FirstOrDefault();
                    
                    if (monitoringService != null)
                    {
                        await monitoringService.TrackDeployment(deploymentName, userName, startTime, model.SelectedSubscriptionId!, model.SelectedResourceGroupName!);
                    }

                    model.DeploymentStatus = "started";
                    model.DeploymentMessage = "Deployment started successfully! You'll receive notifications as it progresses.";
                    model.DeploymentName = deploymentName;
                }
                else
                {
                    model.DeploymentStatus = "error";
                    model.DeploymentMessage = result.Error ?? "Failed to start deployment";
                    model.DeploymentName = deploymentName;
                }
            }
            catch (Exception ex)
            {
                model.DeploymentStatus = "error";
                model.DeploymentMessage = ex.Message;
            }

            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Status(string deploymentName, string subscriptionId, string resourceGroupName)
        {
            var authResult = CheckAuthorizationIfConfigured();
            if (authResult != null) return authResult;

            if (string.IsNullOrEmpty(deploymentName))
            {
                return BadRequest("Deployment name is required");
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return BadRequest("Subscription ID is required");
            }

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                return BadRequest("Resource group name is required");
            }

            try
            {
                var status = await _deploymentService.GetDeploymentStatusAsync(deploymentName, subscriptionId, resourceGroupName);
                
                var statusModel = new DeploymentStatusViewModel
                {
                    DeploymentName = deploymentName,
                    Status = status,
                    ResourceGroup = resourceGroupName,
                    IsSuccessful = status == "Succeeded",
                    IsRunning = status == "Running" || status == "Accepted",
                    HasError = status == "Failed" || status == "Canceled"
                };

                return Json(statusModel);
            }
            catch (Exception ex)
            {
                return Json(new DeploymentStatusViewModel
                {
                    DeploymentName = deploymentName,
                    Status = "Failed",
                    Message = ex.Message,
                    HasError = true
                });
            }
        }
    }
}