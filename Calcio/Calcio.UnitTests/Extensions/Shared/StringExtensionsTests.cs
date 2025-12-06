using Calcio.Shared.Extensions.Shared;

using Shouldly;

namespace Calcio.UnitTests.Extensions.Shared;

public class StringExtensionsTests
{
    #region ContainsIgnoreCase Tests

    [Fact]
    public void ContainsIgnoreCase_WhenSourceContainsValueWithSameCase_ReturnsTrue()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.ContainsIgnoreCase("World");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenSourceContainsValueWithDifferentCase_ReturnsTrue()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.ContainsIgnoreCase("WORLD");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenSourceDoesNotContainValue_ReturnsFalse()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.ContainsIgnoreCase("Foo");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenSourceIsNull_ReturnsFalse()
    {
        // Arrange
        string? source = null;

        // Act
        var result = source.ContainsIgnoreCase("value");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenValueIsNull_ReturnsFalse()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.ContainsIgnoreCase(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenBothAreNull_ReturnsFalse()
    {
        // Arrange
        string? source = null;

        // Act
        var result = source.ContainsIgnoreCase(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenValueIsEmpty_ReturnsTrue()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.ContainsIgnoreCase(string.Empty);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ContainsIgnoreCase_WhenSourceIsEmpty_ReturnsFalse()
    {
        // Arrange
        string source = string.Empty;

        // Act
        var result = source.ContainsIgnoreCase("value");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region EqualsIgnoreCase Tests

    [Fact]
    public void EqualsIgnoreCase_WhenValuesAreEqualWithSameCase_ReturnsTrue()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.EqualsIgnoreCase("Hello World");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenValuesAreEqualWithDifferentCase_ReturnsTrue()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.EqualsIgnoreCase("HELLO WORLD");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenValuesAreNotEqual_ReturnsFalse()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.EqualsIgnoreCase("Goodbye World");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenSourceIsNull_ReturnsFalse()
    {
        // Arrange
        string? source = null;

        // Act
        var result = source.EqualsIgnoreCase("value");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenValueIsNull_ReturnsFalse()
    {
        // Arrange
        string source = "Hello World";

        // Act
        var result = source.EqualsIgnoreCase(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenBothAreNull_ReturnsTrue()
    {
        // Arrange
        string? source = null;

        // Act
        var result = source.EqualsIgnoreCase(null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenBothAreEmpty_ReturnsTrue()
    {
        // Arrange
        string source = string.Empty;

        // Act
        var result = source.EqualsIgnoreCase(string.Empty);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualsIgnoreCase_WhenSourceIsEmptyAndValueIsNot_ReturnsFalse()
    {
        // Arrange
        string source = string.Empty;

        // Act
        var result = source.EqualsIgnoreCase("value");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
