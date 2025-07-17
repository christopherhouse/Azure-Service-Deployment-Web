using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class ServiceBusDeploymentQueueServiceTests
{
    private readonly Mock<ILogger<ServiceBusDeploymentQueueService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ServiceBusOptions _serviceBusOptions;

    public ServiceBusDeploymentQueueServiceTests()
    {
        _mockLogger = new Mock<ILogger<ServiceBusDeploymentQueueService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _serviceBusOptions = new ServiceBusOptions
        {
            NamespaceEndpoint = "", // Empty to force fallback mode
            ClientId = "",
            TopicName = "deployments",
            SubscriptionName = "all-messages"
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WithEmptyNamespaceEndpoint_ShouldBeDisabled()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);

        // Act
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Assert
        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EnqueueJob_WhenDisabled_ShouldUseLocalQueue()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);
        var job = CreateTestDeploymentJob();

        // Act
        service.EnqueueJob(job);

        // Assert
        service.GetQueueCount().Should().Be(1);
        var pendingJobs = service.GetPendingJobs();
        pendingJobs.Should().ContainSingle();
        pendingJobs.First().JobId.Should().Be(job.JobId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryDequeueJob_WhenDisabled_ShouldUseLocalQueue()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);
        var job = CreateTestDeploymentJob();
        service.EnqueueJob(job);

        // Act
        var result = service.TryDequeueJob(out var dequeuedJob);

        // Assert
        result.Should().BeTrue();
        dequeuedJob.Should().NotBeNull();
        dequeuedJob!.JobId.Should().Be(job.JobId);
        service.GetQueueCount().Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryDequeueJob_WhenQueueEmpty_ShouldReturnFalse()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var result = service.TryDequeueJob(out var job);

        // Assert
        result.Should().BeFalse();
        job.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetQueueCount_InitiallyEmpty_ShouldReturnZero()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var count = service.GetQueueCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetPendingJobs_InitiallyEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var jobs = service.GetPendingJobs();

        // Assert
        jobs.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EnqueueJob_WithNullJob_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);
        DeploymentJob? job = null;

        // Act & Assert
        Action act = () => service.EnqueueJob(job!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("job");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EnqueueJob_MultipleJobs_ShouldMaintainOrder()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);
        var job1 = CreateTestDeploymentJob("test-deployment-1", "test-user-1");
        var job2 = CreateTestDeploymentJob("test-deployment-2", "test-user-2");

        // Act
        service.EnqueueJob(job1);
        service.EnqueueJob(job2);

        // Assert
        service.GetQueueCount().Should().Be(2);
        
        var result1 = service.TryDequeueJob(out var dequeuedJob1);
        var result2 = service.TryDequeueJob(out var dequeuedJob2);
        
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        dequeuedJob1!.JobId.Should().Be(job1.JobId);
        dequeuedJob2!.JobId.Should().Be(job2.JobId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartProcessingAsync_WhenDisabled_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.StartProcessingAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StopProcessingAsync_WhenDisabled_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.StopProcessingAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_serviceBusOptions);
        var service = new ServiceBusDeploymentQueueService(options, _mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.DisposeAsync().AsTask())
            .Should().NotThrowAsync();
    }

    private static DeploymentJob CreateTestDeploymentJob(string deploymentName = "test-deployment", string userName = "test-user")
    {
        return new DeploymentJob
        {
            JobId = Guid.NewGuid(),
            TemplateContent = "{ \"$schema\": \"test\" }",
            ParametersContent = "{ \"parameters\": {} }",
            DeploymentName = deploymentName,
            SubscriptionId = "test-subscription-id",
            ResourceGroupName = "test-resource-group",
            UserName = userName,
            StartTime = DateTime.UtcNow
        };
    }
}