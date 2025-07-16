using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class AzureResourceDiscoveryServiceTests : IDisposable
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IOptions<CacheOptions>> _mockCacheOptions;
    private readonly Mock<IOptions<AzureAdOptions>> _mockAzureAdOptions;
    private readonly Mock<ILogger<AzureResourceDiscoveryService>> _mockLogger;
    private readonly CacheOptions _cacheOptions;
    private readonly AzureAdOptions _azureAdOptions;

    public AzureResourceDiscoveryServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockCacheOptions = new Mock<IOptions<CacheOptions>>();
        _mockAzureAdOptions = new Mock<IOptions<AzureAdOptions>>();
        _mockLogger = new Mock<ILogger<AzureResourceDiscoveryService>>();

        _cacheOptions = new CacheOptions
        {
            SubscriptionsCacheDurationMinutes = 30,
            ResourceGroupsCacheDurationMinutes = 15
        };

        _azureAdOptions = new AzureAdOptions
        {
            // These would normally be set from configuration
            // For testing purposes, we can use mock values
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TenantId = "test-tenant-id"
        };

        _mockCacheOptions.Setup(x => x.Value).Returns(_cacheOptions);
        _mockAzureAdOptions.Setup(x => x.Value).Returns(_azureAdOptions);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureResourceDiscoveryService(null!, _mockCacheOptions.Object, _mockAzureAdOptions.Object, _mockLogger.Object));
        
        exception.ParamName.Should().Be("cache");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullCacheOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureResourceDiscoveryService(_mockCache.Object, null!, _mockAzureAdOptions.Object, _mockLogger.Object));
        
        exception.ParamName.Should().Be("cacheOptions");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullAzureAdOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureResourceDiscoveryService(_mockCache.Object, _mockCacheOptions.Object, null!, _mockLogger.Object));
        
        exception.ParamName.Should().Be("azureAdOptions");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureResourceDiscoveryService(_mockCache.Object, _mockCacheOptions.Object, _mockAzureAdOptions.Object, null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new AzureResourceDiscoveryService(
            _mockCache.Object, 
            _mockCacheOptions.Object, 
            _mockAzureAdOptions.Object, 
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    public void Dispose()
    {
        // Clean up any resources if needed
    }
}

public class SubscriptionInfoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void SubscriptionInfo_Properties_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var subscriptionInfo = new SubscriptionInfo
        {
            SubscriptionId = "test-sub-id",
            DisplayName = "Test Subscription"
        };

        // Assert
        subscriptionInfo.SubscriptionId.Should().Be("test-sub-id");
        subscriptionInfo.DisplayName.Should().Be("Test Subscription");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SubscriptionInfo_DefaultValues_ShouldBeEmptyStrings()
    {
        // Arrange & Act
        var subscriptionInfo = new SubscriptionInfo();

        // Assert
        subscriptionInfo.SubscriptionId.Should().Be(string.Empty);
        subscriptionInfo.DisplayName.Should().Be(string.Empty);
    }
}

public class ResourceGroupInfoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void ResourceGroupInfo_Properties_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var resourceGroupInfo = new ResourceGroupInfo
        {
            Name = "test-rg",
            Location = "East US"
        };

        // Assert
        resourceGroupInfo.Name.Should().Be("test-rg");
        resourceGroupInfo.Location.Should().Be("East US");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ResourceGroupInfo_DefaultValues_ShouldBeEmptyStrings()
    {
        // Arrange & Act
        var resourceGroupInfo = new ResourceGroupInfo();

        // Assert
        resourceGroupInfo.Name.Should().Be(string.Empty);
        resourceGroupInfo.Location.Should().Be(string.Empty);
    }
}