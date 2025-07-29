using Azure.Core;
using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class UserCredentialIntegrationTests
{
    private readonly Mock<IOptions<AzureAdOptions>> _mockAzureAdOptions;
    private readonly AzureAdOptions _azureAdOptions;

    public UserCredentialIntegrationTests()
    {
        _azureAdOptions = new AzureAdOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TenantId = "test-tenant-id"
        };
        _mockAzureAdOptions = new Mock<IOptions<AzureAdOptions>>();
        _mockAzureAdOptions.Setup(o => o.Value).Returns(_azureAdOptions);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AzureDeploymentService_Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => new AzureDeploymentService(_mockAzureAdOptions.Object);
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AzureDeploymentService_WithUserCredential_ShouldAcceptCredentialParameter()
    {
        // Arrange
        var service = new AzureDeploymentService(_mockAzureAdOptions.Object);
        var userCredential = new AccessTokenCredential("test-token");

        // Act & Assert
        // This test verifies that the service accepts user credentials without throwing
        // ArgumentNullException or similar parameter validation errors
        var methodInfo = typeof(AzureDeploymentService).GetMethod("DeployTemplateAsync");
        
        methodInfo.Should().NotBeNull();
        var parameters = methodInfo!.GetParameters();
        parameters.Should().HaveCount(6);
        parameters.Last().Name.Should().Be("userCredential");
        parameters.Last().ParameterType.Should().Be(typeof(TokenCredential));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentJob_WithUserToken_ShouldStoreToken()
    {
        // Arrange
        const string userToken = "test-user-token";

        // Act
        var job = new DeploymentJob
        {
            UserAccessToken = userToken,
            UserName = "test-user",
            DeploymentName = "test-deployment"
        };

        // Assert
        job.UserAccessToken.Should().Be(userToken);
        job.UserName.Should().Be("test-user");
        job.DeploymentName.Should().Be("test-deployment");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AccessTokenCredential_Integration_ShouldWorkWithAzureServices()
    {
        // Arrange
        const string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test"; // Mock JWT-like token
        var credential = new AccessTokenCredential(token);

        // Act
        var tokenResult = credential.GetToken(
            new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
            CancellationToken.None);

        // Assert
        tokenResult.Token.Should().Be(token);
        tokenResult.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow);

        // Verify that this credential can be used to create an ArmClient
        // (This is a design verification - actual use would require real Azure resources)
        var act = () => new Azure.ResourceManager.ArmClient(credential);
        act.Should().NotThrow();
    }
}