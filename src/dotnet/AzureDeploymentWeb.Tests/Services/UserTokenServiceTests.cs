using Azure.Core;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class UserTokenServiceTests
{
    private readonly Mock<ITokenAcquisition> _mockTokenAcquisition;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<UserTokenService>> _mockLogger;
    private readonly UserTokenService _service;

    public UserTokenServiceTests()
    {
        _mockTokenAcquisition = new Mock<ITokenAcquisition>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<UserTokenService>>();

        _service = new UserTokenService(
            _mockTokenAcquisition.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAccessTokenAsync_WhenAuthenticationNotConfigured_ShouldReturnNull()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["AzureAd:ClientId"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["AzureAd:ClientSecret"]).Returns((string?)null);

        // Act
        var result = await _service.GetAccessTokenAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAccessTokenAsync_WhenUserNotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["AzureAd:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(c => c["AzureAd:ClientSecret"]).Returns("test-client-secret");

        var mockHttpContext = new Mock<HttpContext>();
        var mockUser = new Mock<ClaimsPrincipal>();
        var mockIdentity = new Mock<ClaimsIdentity>();
        
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(false);
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = await _service.GetAccessTokenAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserTokenCredentialAsync_WhenTokenNotAvailable_ShouldReturnNull()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["AzureAd:ClientId"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["AzureAd:ClientSecret"]).Returns((string?)null);

        // Act
        var result = await _service.GetUserTokenCredentialAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UserTokenService_Constructor_ShouldNotThrow()
    {
        // This test verifies the service can be constructed without issues
        var service = new UserTokenService(
            _mockTokenAcquisition.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        service.Should().NotBeNull();
    }
}

public class AccessTokenCredentialTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AccessTokenCredential(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetToken_ShouldReturnAccessTokenWithValidExpiry()
    {
        // Arrange
        const string token = "test-token";
        var credential = new AccessTokenCredential(token);
        var requestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });

        // Act
        var result = credential.GetToken(requestContext, CancellationToken.None);

        // Assert
        result.Token.Should().Be(token);
        result.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow);
        result.ExpiresOn.Should().BeBefore(DateTimeOffset.UtcNow.AddHours(2));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTokenAsync_ShouldReturnAccessTokenWithValidExpiry()
    {
        // Arrange
        const string token = "test-token";
        var credential = new AccessTokenCredential(token);
        var requestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });

        // Act
        var result = await credential.GetTokenAsync(requestContext, CancellationToken.None);

        // Assert
        result.Token.Should().Be(token);
        result.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow);
        result.ExpiresOn.Should().BeBefore(DateTimeOffset.UtcNow.AddHours(2));
    }
}