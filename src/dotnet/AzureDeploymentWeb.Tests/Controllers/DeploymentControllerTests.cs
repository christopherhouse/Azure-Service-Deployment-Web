using AzureDeploymentWeb.Controllers;
using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace AzureDeploymentWeb.Tests.Controllers;

public class DeploymentControllerTests
{
    private readonly Mock<IAzureDeploymentService> _mockDeploymentService;
    private readonly Mock<IDeploymentQueueService> _mockQueueService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly DeploymentController _controller;

    public DeploymentControllerTests()
    {
        _mockDeploymentService = new Mock<IAzureDeploymentService>();
        _mockQueueService = new Mock<IDeploymentQueueService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfiguration = new Mock<IConfiguration>();

        _controller = new DeploymentController(
            _mockDeploymentService.Object,
            _mockQueueService.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Index_ShouldReturnViewWithModel()
    {
        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.Model.Should().BeOfType<DeploymentViewModel>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Deploy_WithValidModel_ShouldEnqueueJobAndReturnSuccess()
    {
        // Arrange
        var model = CreateValidDeploymentViewModel();
        DeploymentJob? enqueuedJob = null;
        
        _mockQueueService.Setup(qs => qs.EnqueueJob(It.IsAny<DeploymentJob>()))
            .Callback<DeploymentJob>(job => enqueuedJob = job);

        // Act
        var result = await _controller.Deploy(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.ViewName.Should().Be("Index");
        
        var resultModel = viewResult.Model.Should().BeOfType<DeploymentViewModel>().Subject;
        resultModel.DeploymentStatus.Should().Be(DeploymentStatus.Queued);
        resultModel.DeploymentMessage.Should().Contain("queued");
        resultModel.DeploymentName.Should().NotBeNullOrEmpty();

        // Verify job was enqueued
        _mockQueueService.Verify(qs => qs.EnqueueJob(It.IsAny<DeploymentJob>()), Times.Once);
        enqueuedJob.Should().NotBeNull();
        enqueuedJob!.TemplateContent.Should().NotBeEmpty();
        enqueuedJob.ParametersContent.Should().NotBeEmpty();
        enqueuedJob.SubscriptionId.Should().Be(model.SelectedSubscriptionId);
        enqueuedJob.ResourceGroupName.Should().Be(model.SelectedResourceGroupName);
        enqueuedJob.UserName.Should().Be("test-user");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Deploy_WithInvalidModel_ShouldReturnViewWithErrors()
    {
        // Arrange
        var model = new DeploymentViewModel(); // Invalid model - missing required fields
        _controller.ModelState.AddModelError("TemplateFile", "Required");

        // Act
        var result = await _controller.Deploy(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = (ViewResult)result;
        viewResult.ViewName.Should().Be("Index");
        viewResult.Model.Should().Be(model);

        // Verify no job was enqueued
        _mockQueueService.Verify(qs => qs.EnqueueJob(It.IsAny<DeploymentJob>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Status_WithValidParameters_ShouldReturnJsonResult()
    {
        // Arrange
        const string deploymentName = "test-deployment";
        const string subscriptionId = "test-subscription";
        const string resourceGroupName = "test-rg";
        const DeploymentStatus status = DeploymentStatus.Running;

        _mockDeploymentService.Setup(ds => ds.GetDeploymentStatusAsync(deploymentName, subscriptionId, resourceGroupName))
            .ReturnsAsync(status);

        // Act
        var result = await _controller.Status(deploymentName, subscriptionId, resourceGroupName);

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = (JsonResult)result;
        var statusModel = jsonResult.Value.Should().BeOfType<DeploymentStatusViewModel>().Subject;
        
        statusModel.DeploymentName.Should().Be(deploymentName);
        statusModel.Status.Should().Be(status);
        statusModel.ResourceGroup.Should().Be(resourceGroupName);
        statusModel.IsRunning.Should().BeTrue();
        statusModel.IsSuccessful.Should().BeFalse();
        statusModel.HasError.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Status_WithEmptyDeploymentName_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Status("", "subscription", "rg");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().Be("Deployment name is required");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Status_WithServiceException_ShouldReturnErrorStatus()
    {
        // Arrange
        const string deploymentName = "test-deployment";
        const string subscriptionId = "test-subscription";
        const string resourceGroupName = "test-rg";

        _mockDeploymentService.Setup(ds => ds.GetDeploymentStatusAsync(deploymentName, subscriptionId, resourceGroupName))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.Status(deploymentName, subscriptionId, resourceGroupName);

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = (JsonResult)result;
        var statusModel = jsonResult.Value.Should().BeOfType<DeploymentStatusViewModel>().Subject;
        
        statusModel.Status.Should().Be(DeploymentStatus.Failed);
        statusModel.HasError.Should().BeTrue();
        statusModel.Message.Should().Be("Service error");
    }

    private static DeploymentViewModel CreateValidDeploymentViewModel()
    {
        // Create mock files
        var templateContent = "{ 'template': 'content' }";
        var parametersContent = "{ 'parameters': 'content' }";
        
        var templateStream = new MemoryStream(Encoding.UTF8.GetBytes(templateContent));
        var parametersStream = new MemoryStream(Encoding.UTF8.GetBytes(parametersContent));
        
        var templateFile = new Mock<IFormFile>();
        templateFile.Setup(f => f.OpenReadStream()).Returns(templateStream);
        templateFile.Setup(f => f.FileName).Returns("template.json");
        
        var parametersFile = new Mock<IFormFile>();
        parametersFile.Setup(f => f.OpenReadStream()).Returns(parametersStream);
        parametersFile.Setup(f => f.FileName).Returns("parameters.json");

        return new DeploymentViewModel
        {
            TemplateFile = templateFile.Object,
            ParametersFile = parametersFile.Object,
            SelectedSubscriptionId = "test-subscription-id",
            SelectedResourceGroupName = "test-rg"
        };
    }
}