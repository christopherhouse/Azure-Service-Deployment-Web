using System.ComponentModel.DataAnnotations;
using AzureDeploymentWeb.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AzureDeploymentWeb.Tests.Models;

public class DeploymentViewModelTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_WhenTemplateFileIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            TemplateFile = null,
            ParametersFile = Mock.Of<IFormFile>(),
            SelectedSubscriptionId = "sub-123",
            SelectedResourceGroupName = "rg-test"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(DeploymentViewModel.TemplateFile)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_WhenParametersFileIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            TemplateFile = Mock.Of<IFormFile>(),
            ParametersFile = null,
            SelectedSubscriptionId = "sub-123",
            SelectedResourceGroupName = "rg-test"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(DeploymentViewModel.ParametersFile)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_WhenSubscriptionIdIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            TemplateFile = Mock.Of<IFormFile>(),
            ParametersFile = Mock.Of<IFormFile>(),
            SelectedSubscriptionId = null,
            SelectedResourceGroupName = "rg-test"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(DeploymentViewModel.SelectedSubscriptionId)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_WhenResourceGroupNameIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            TemplateFile = Mock.Of<IFormFile>(),
            ParametersFile = Mock.Of<IFormFile>(),
            SelectedSubscriptionId = "sub-123",
            SelectedResourceGroupName = null
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(DeploymentViewModel.SelectedResourceGroupName)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_WhenAllRequiredFieldsProvided_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            TemplateFile = Mock.Of<IFormFile>(),
            ParametersFile = Mock.Of<IFormFile>(),
            SelectedSubscriptionId = "sub-123",
            SelectedResourceGroupName = "rg-test"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentViewModel_BackwardsCompatibilityProperties_ShouldReturnSelectedValues()
    {
        // Arrange
        var model = new DeploymentViewModel
        {
            SelectedSubscriptionId = "sub-123",
            SelectedResourceGroupName = "rg-test"
        };

        // Act & Assert
        model.SubscriptionId.Should().Be("sub-123");
        model.ResourceGroup.Should().Be("rg-test");
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }
}

public class DeploymentNotificationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_WhenStatusIsSucceeded_IsSuccessfulShouldBeTrue()
    {
        // Arrange
        var notification = new DeploymentNotification { Status = DeploymentStatus.Succeeded };

        // Act & Assert
        notification.IsSuccessful.Should().BeTrue();
        notification.IsRunning.Should().BeFalse();
        notification.HasError.Should().BeFalse();
        notification.IsCompleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_WhenStatusIsFailed_HasErrorShouldBeTrue()
    {
        // Arrange
        var notification = new DeploymentNotification { Status = DeploymentStatus.Failed };

        // Act & Assert
        notification.IsSuccessful.Should().BeFalse();
        notification.IsRunning.Should().BeFalse();
        notification.HasError.Should().BeTrue();
        notification.IsCompleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_WhenStatusIsRunning_IsRunningShouldBeTrue()
    {
        // Arrange
        var notification = new DeploymentNotification { Status = DeploymentStatus.Running };

        // Act & Assert
        notification.IsSuccessful.Should().BeFalse();
        notification.IsRunning.Should().BeTrue();
        notification.HasError.Should().BeFalse();
        notification.IsCompleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(DeploymentStatus.Running)]
    [InlineData(DeploymentStatus.Accepted)]
    [InlineData(DeploymentStatus.Started)]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_WhenStatusIsInProgress_IsRunningShouldBeTrue(DeploymentStatus status)
    {
        // Arrange
        var notification = new DeploymentNotification { Status = status };

        // Act & Assert
        notification.IsRunning.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);
        var notification = new DeploymentNotification 
        { 
            StartTime = startTime,
            EndTime = endTime
        };

        // Act & Assert
        notification.Duration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeploymentNotification_DurationWhenEndTimeIsNull_ShouldBeNull()
    {
        // Arrange
        var notification = new DeploymentNotification 
        { 
            StartTime = DateTime.UtcNow,
            EndTime = null
        };

        // Act & Assert
        notification.Duration.Should().BeNull();
    }
}