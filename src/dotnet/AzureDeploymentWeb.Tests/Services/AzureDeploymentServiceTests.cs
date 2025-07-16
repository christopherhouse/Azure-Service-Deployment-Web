using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class DeploymentResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentResult_Properties_ShouldBeInitializedCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);

        // Act
        var result = new DeploymentResult
        {
            Success = true,
            DeploymentName = "test-deployment",
            ResourceGroupName = "test-rg",
            Outputs = new { storageAccountId = "test-id" },
            Error = null,
            StartTime = startTime,
            EndTime = endTime
        };

        // Assert
        result.Success.Should().BeTrue();
        result.DeploymentName.Should().Be("test-deployment");
        result.ResourceGroupName.Should().Be("test-rg");
        result.Outputs.Should().NotBeNull();
        result.Error.Should().BeNull();
        result.StartTime.Should().Be(startTime);
        result.EndTime.Should().Be(endTime);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentResult_WhenError_SuccessShouldBeFalse()
    {
        // Arrange & Act
        var result = new DeploymentResult
        {
            Success = false,
            Error = "Deployment failed due to invalid template",
            StartTime = DateTime.UtcNow
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Deployment failed due to invalid template");
        result.EndTime.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var result = new DeploymentResult();

        // Assert
        result.Success.Should().BeFalse();
        result.DeploymentName.Should().BeNull();
        result.ResourceGroupName.Should().BeNull();
        result.Outputs.Should().BeNull();
        result.Error.Should().BeNull();
        result.StartTime.Should().Be(default(DateTime));
        result.EndTime.Should().BeNull();
    }
}

public class AzureDeploymentServiceTests : IDisposable
{
    private readonly Mock<IOptions<AzureAdOptions>> _mockAzureAdOptions;
    private readonly AzureAdOptions _azureAdOptions;

    public AzureDeploymentServiceTests()
    {
        _mockAzureAdOptions = new Mock<IOptions<AzureAdOptions>>();
        _azureAdOptions = new AzureAdOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TenantId = "test-tenant-id"
        };
        _mockAzureAdOptions.Setup(x => x.Value).Returns(_azureAdOptions);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullAzureAdOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureDeploymentService(null!));
        
        exception.ParamName.Should().Be("azureAdOptions");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new AzureDeploymentService(_mockAzureAdOptions.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAzureDeploymentService>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullAzureAdOptionsValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<AzureAdOptions>>();
        mockOptions.Setup(x => x.Value).Returns((AzureAdOptions)null!);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureDeploymentService(mockOptions.Object));
        
        exception.ParamName.Should().Be("azureAdOptions");
    }

    public void Dispose()
    {
        // Clean up any resources if needed
    }
}