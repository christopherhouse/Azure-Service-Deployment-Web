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
        private readonly IDeploymentQueueService _queueService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DeploymentController(
            IAzureDeploymentService deploymentService,
            IDeploymentQueueService queueService,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _deploymentService = deploymentService;
            _queueService = queueService;
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
                var userName = User.Identity?.Name ?? "unknown";

                // Create deployment job
                var deploymentJob = new DeploymentJob
                {
                    TemplateContent = templateContent,
                    ParametersContent = parametersContent,
                    DeploymentName = deploymentName,
                    SubscriptionId = model.SelectedSubscriptionId!,
                    ResourceGroupName = model.SelectedResourceGroupName!,
                    UserName = userName,
                    StartTime = startTime
                };

                // Enqueue the deployment job instead of starting it directly
                _queueService.EnqueueJob(deploymentJob);

                model.DeploymentStatus = Models.DeploymentStatus.Queued;
                model.DeploymentMessage = "Deployment has been queued and will start processing shortly. You'll receive notifications as it progresses.";
                model.DeploymentName = deploymentName;
            }
            catch (Exception ex)
            {
                model.DeploymentStatus = Models.DeploymentStatus.Error;
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
                    IsSuccessful = status == Models.DeploymentStatus.Succeeded,
                    IsRunning = status == Models.DeploymentStatus.Running || status == Models.DeploymentStatus.Accepted,
                    HasError = status == Models.DeploymentStatus.Failed || status == Models.DeploymentStatus.Canceled
                };

                return Json(statusModel);
            }
            catch (Exception ex)
            {
                return Json(new DeploymentStatusViewModel
                {
                    DeploymentName = deploymentName,
                    Status = Models.DeploymentStatus.Failed,
                    Message = ex.Message,
                    HasError = true
                });
            }
        }
    }
}