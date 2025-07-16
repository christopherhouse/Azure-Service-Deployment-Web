using FluentAssertions;
using Xunit;

namespace AzureDeploymentWeb.Tests.Extensions;

public class ExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringContainsNewlines_ShouldRemoveThem()
    {
        // Arrange
        var input = "Hello\nWorld\r\nTest";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().Be("HelloWorldTest");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringContainsTabs_ShouldRemoveThem()
    {
        // Arrange
        var input = "Hello\tWorld\tTest";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().Be("HelloWorldTest");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringContainsMixedWhitespace_ShouldRemoveAllSpecialChars()
    {
        // Arrange
        var input = "Hello\n\r\tWorld\n\tTest\r";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().Be("HelloWorldTest");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var input = "";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringIsWhitespace_ShouldReturnNull()
    {
        // Arrange
        var input = "   ";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SanitizeString_WhenStringIsValid_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Hello World Test";
        
        // Act
        var result = input.SanitizeString();
        
        // Assert
        result.Should().Be("Hello World Test");
    }
}