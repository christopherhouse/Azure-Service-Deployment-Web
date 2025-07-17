using AzureDeploymentWeb.Models;
using FluentAssertions;
using Xunit;

namespace AzureDeploymentWeb.Tests.Models;

public class DeploymentJobTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentJob_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var job = new DeploymentJob();

        // Assert
        job.JobId.Should().NotBeEmpty();
        job.TemplateContent.Should().BeEmpty();
        job.ParametersContent.Should().BeEmpty();
        job.DeploymentName.Should().BeEmpty();
        job.SubscriptionId.Should().BeEmpty();
        job.ResourceGroupName.Should().BeEmpty();
        job.UserName.Should().BeEmpty();
        job.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentJob_Properties_ShouldBeSettable()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        const string templateContent = "{ 'template': 'content' }";
        const string parametersContent = "{ 'parameters': 'content' }";
        const string deploymentName = "test-deployment";
        const string subscriptionId = "test-subscription-id";
        const string resourceGroupName = "test-rg";
        const string userName = "test-user";

        // Act
        var job = new DeploymentJob
        {
            JobId = jobId,
            TemplateContent = templateContent,
            ParametersContent = parametersContent,
            DeploymentName = deploymentName,
            SubscriptionId = subscriptionId,
            ResourceGroupName = resourceGroupName,
            UserName = userName,
            StartTime = startTime
        };

        // Assert
        job.JobId.Should().Be(jobId);
        job.TemplateContent.Should().Be(templateContent);
        job.ParametersContent.Should().Be(parametersContent);
        job.DeploymentName.Should().Be(deploymentName);
        job.SubscriptionId.Should().Be(subscriptionId);
        job.ResourceGroupName.Should().Be(resourceGroupName);
        job.UserName.Should().Be(userName);
        job.StartTime.Should().Be(startTime);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentJob_JobId_ShouldBeUniqueForEachInstance()
    {
        // Act
        var job1 = new DeploymentJob();
        var job2 = new DeploymentJob();

        // Assert
        job1.JobId.Should().NotBe(job2.JobId);
        job1.JobId.Should().NotBeEmpty();
        job2.JobId.Should().NotBeEmpty();
    }
}