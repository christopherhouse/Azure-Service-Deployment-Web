using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using System.Text;

namespace AzureDeploymentWeb.Controllers
{
    [Authorize]
    public class DeploymentController : Controller
    {
        private readonly IAzureDeploymentService _deploymentService;
        private readonly IConfiguration _configuration;

        public DeploymentController(IAzureDeploymentService deploymentService, IConfiguration configuration)
        {
            _deploymentService = deploymentService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var model = new DeploymentViewModel
            {
                ResourceGroup = _configuration["Azure:ResourceGroup"],
                SubscriptionId = _configuration["Azure:SubscriptionId"]
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deploy(DeploymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResourceGroup = _configuration["Azure:ResourceGroup"];
                model.SubscriptionId = _configuration["Azure:SubscriptionId"];
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

                // Deploy template
                var result = await _deploymentService.DeployTemplateAsync(templateContent, parametersContent, deploymentName);

                if (result.Success)
                {
                    model.DeploymentStatus = "success";
                    model.DeploymentMessage = "Resources deployed successfully!";
                    model.DeploymentName = deploymentName;
                }
                else
                {
                    model.DeploymentStatus = "error";
                    model.DeploymentMessage = result.Error ?? "Unknown error occurred";
                    model.DeploymentName = deploymentName;
                }
            }
            catch (Exception ex)
            {
                model.DeploymentStatus = "error";
                model.DeploymentMessage = ex.Message;
            }

            model.ResourceGroup = _configuration["Azure:ResourceGroup"];
            model.SubscriptionId = _configuration["Azure:SubscriptionId"];
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Status(string deploymentName)
        {
            if (string.IsNullOrEmpty(deploymentName))
            {
                return BadRequest("Deployment name is required");
            }

            try
            {
                var status = await _deploymentService.GetDeploymentStatusAsync(deploymentName);
                
                var statusModel = new DeploymentStatusViewModel
                {
                    DeploymentName = deploymentName,
                    Status = status,
                    ResourceGroup = _configuration["Azure:ResourceGroup"],
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