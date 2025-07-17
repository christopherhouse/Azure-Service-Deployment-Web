using AzureDeploymentWeb.Models;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;

namespace AzureDeploymentWeb.Tests.Integration;

public class DeploymentWorkflowIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void DeploymentWorkflow_ShouldProcessJobsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IDeploymentQueueService, DeploymentQueueService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var queueService = serviceProvider.GetRequiredService<IDeploymentQueueService>();
        
        var job1 = new DeploymentJob
        {
            TemplateContent = "{ 'template1': 'content' }",
            ParametersContent = "{ 'parameters1': 'content' }",
            DeploymentName = "test-deployment-1",
            SubscriptionId = "test-subscription-id",
            ResourceGroupName = "test-rg",
            UserName = "test-user-1",
            StartTime = DateTime.UtcNow
        };

        var job2 = new DeploymentJob
        {
            TemplateContent = "{ 'template2': 'content' }",
            ParametersContent = "{ 'parameters2': 'content' }",
            DeploymentName = "test-deployment-2",
            SubscriptionId = "test-subscription-id",
            ResourceGroupName = "test-rg",
            UserName = "test-user-2",
            StartTime = DateTime.UtcNow
        };

        // Act
        queueService.EnqueueJob(job1);
        queueService.EnqueueJob(job2);

        // Assert
        queueService.GetQueueCount().Should().Be(2);

        var pendingJobs = queueService.GetPendingJobs().ToList();
        pendingJobs.Should().HaveCount(2);
        pendingJobs.Should().Contain(j => j.DeploymentName == "test-deployment-1");
        pendingJobs.Should().Contain(j => j.DeploymentName == "test-deployment-2");

        // Verify jobs can be dequeued in order
        var result1 = queueService.TryDequeueJob(out var dequeuedJob1);
        result1.Should().BeTrue();
        dequeuedJob1!.DeploymentName.Should().Be("test-deployment-1");

        var result2 = queueService.TryDequeueJob(out var dequeuedJob2);
        result2.Should().BeTrue();
        dequeuedJob2!.DeploymentName.Should().Be("test-deployment-2");

        queueService.GetQueueCount().Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void QueueService_ShouldBeSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDeploymentQueueService, DeploymentQueueService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var queueService1 = serviceProvider.GetRequiredService<IDeploymentQueueService>();
        var queueService2 = serviceProvider.GetRequiredService<IDeploymentQueueService>();

        // Assert
        queueService1.Should().BeSameAs(queueService2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void DeploymentJob_UniqueIds_ShouldBeGenerated()
    {
        // Arrange & Act
        var jobs = Enumerable.Range(0, 10)
            .Select(_ => new DeploymentJob())
            .ToList();

        // Assert
        var uniqueIds = jobs.Select(j => j.JobId).Distinct().ToList();
        uniqueIds.Should().HaveCount(10);
        jobs.Should().OnlyContain(j => j.JobId != Guid.Empty);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcurrentOperations_ShouldBeSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDeploymentQueueService, DeploymentQueueService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var queueService = serviceProvider.GetRequiredService<IDeploymentQueueService>();

        const int numberOfThreads = 10;
        const int jobsPerThread = 10;
        
        // Act
        var enqueueTasks = Enumerable.Range(0, numberOfThreads)
            .Select(threadIndex => Task.Run(() =>
            {
                for (int i = 0; i < jobsPerThread; i++)
                {
                    var job = new DeploymentJob
                    {
                        DeploymentName = $"deployment-{threadIndex}-{i}",
                        TemplateContent = "{}",
                        ParametersContent = "{}",
                        SubscriptionId = "test-sub",
                        ResourceGroupName = "test-rg",
                        UserName = "test-user"
                    };
                    queueService.EnqueueJob(job);
                }
            }))
            .ToArray();

        await Task.WhenAll(enqueueTasks);

        // Assert
        queueService.GetQueueCount().Should().Be(numberOfThreads * jobsPerThread);

        // Dequeue all jobs concurrently
        var dequeuedJobs = new List<DeploymentJob>();
        var dequeueTasks = Enumerable.Range(0, numberOfThreads)
            .Select(_ => Task.Run(() =>
            {
                var localJobs = new List<DeploymentJob>();
                while (queueService.TryDequeueJob(out var job) && job != null)
                {
                    localJobs.Add(job);
                }
                lock (dequeuedJobs)
                {
                    dequeuedJobs.AddRange(localJobs);
                }
            }))
            .ToArray();

        await Task.WhenAll(dequeueTasks);

        // Assert all jobs were dequeued
        dequeuedJobs.Should().HaveCount(numberOfThreads * jobsPerThread);
        queueService.GetQueueCount().Should().Be(0);
    }
}