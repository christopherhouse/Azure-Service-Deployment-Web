using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureDeploymentWeb.Tests.Services;

public class DeploymentQueueServiceTests
{
    private readonly Mock<ILogger<DeploymentQueueService>> _mockLogger;
    private readonly DeploymentQueueService _queueService;

    public DeploymentQueueServiceTests()
    {
        _mockLogger = new Mock<ILogger<DeploymentQueueService>>();
        _queueService = new DeploymentQueueService(_mockLogger.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EnqueueJob_WithValidJob_ShouldAddToQueue()
    {
        // Arrange
        var job = CreateTestDeploymentJob();

        // Act
        _queueService.EnqueueJob(job);

        // Assert
        _queueService.GetQueueCount().Should().Be(1);
        var pendingJobs = _queueService.GetPendingJobs();
        pendingJobs.Should().ContainSingle();
        pendingJobs.First().JobId.Should().Be(job.JobId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void EnqueueJob_WithNullJob_ShouldThrowArgumentNullException()
    {
        // Arrange
        DeploymentJob? job = null;

        // Act & Assert
        Action act = () => _queueService.EnqueueJob(job!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("job");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryDequeueJob_WithJobInQueue_ShouldReturnTrueAndJob()
    {
        // Arrange
        var job = CreateTestDeploymentJob();
        _queueService.EnqueueJob(job);

        // Act
        var result = _queueService.TryDequeueJob(out var dequeuedJob);

        // Assert
        result.Should().BeTrue();
        dequeuedJob.Should().NotBeNull();
        dequeuedJob!.JobId.Should().Be(job.JobId);
        _queueService.GetQueueCount().Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryDequeueJob_WithEmptyQueue_ShouldReturnFalseAndNullJob()
    {
        // Act
        var result = _queueService.TryDequeueJob(out var dequeuedJob);

        // Assert
        result.Should().BeFalse();
        dequeuedJob.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetQueueCount_WithMultipleJobs_ShouldReturnCorrectCount()
    {
        // Arrange
        var job1 = CreateTestDeploymentJob();
        var job2 = CreateTestDeploymentJob();
        var job3 = CreateTestDeploymentJob();

        // Act
        _queueService.EnqueueJob(job1);
        _queueService.EnqueueJob(job2);
        _queueService.EnqueueJob(job3);

        // Assert
        _queueService.GetQueueCount().Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetPendingJobs_WithMultipleJobs_ShouldReturnAllJobs()
    {
        // Arrange
        var job1 = CreateTestDeploymentJob("deployment-1");
        var job2 = CreateTestDeploymentJob("deployment-2");
        _queueService.EnqueueJob(job1);
        _queueService.EnqueueJob(job2);

        // Act
        var pendingJobs = _queueService.GetPendingJobs().ToList();

        // Assert
        pendingJobs.Should().HaveCount(2);
        pendingJobs.Should().Contain(j => j.DeploymentName == "deployment-1");
        pendingJobs.Should().Contain(j => j.DeploymentName == "deployment-2");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void QueueOperations_ShouldBeThreadSafe()
    {
        // Arrange
        const int numberOfJobs = 100;
        var tasks = new List<Task>();

        // Act
        // Enqueue jobs from multiple threads
        for (int i = 0; i < numberOfJobs; i++)
        {
            var jobIndex = i;
            tasks.Add(Task.Run(() =>
            {
                var job = CreateTestDeploymentJob($"deployment-{jobIndex}");
                _queueService.EnqueueJob(job);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        _queueService.GetQueueCount().Should().Be(numberOfJobs);

        // Dequeue all jobs from multiple threads
        var dequeuedJobs = new List<DeploymentJob>();
        var dequeueTasks = new List<Task>();

        for (int i = 0; i < numberOfJobs; i++)
        {
            dequeueTasks.Add(Task.Run(() =>
            {
                if (_queueService.TryDequeueJob(out var job) && job != null)
                {
                    lock (dequeuedJobs)
                    {
                        dequeuedJobs.Add(job);
                    }
                }
            }));
        }

        Task.WaitAll(dequeueTasks.ToArray());

        // Assert
        dequeuedJobs.Should().HaveCount(numberOfJobs);
        _queueService.GetQueueCount().Should().Be(0);
    }

    private static DeploymentJob CreateTestDeploymentJob(string? deploymentName = null)
    {
        return new DeploymentJob
        {
            TemplateContent = "{ 'template': 'content' }",
            ParametersContent = "{ 'parameters': 'content' }",
            DeploymentName = deploymentName ?? "test-deployment",
            SubscriptionId = "test-subscription-id",
            ResourceGroupName = "test-rg",
            UserName = "test-user",
            StartTime = DateTime.UtcNow
        };
    }
}