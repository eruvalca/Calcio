using Calcio.Shared.Extensions.Shared;

using Shouldly;

namespace Calcio.UnitTests.Extensions.Shared;

/// <summary>
/// Contains unit tests for S tr in gE xt en si on s behavior.
/// </summary>
public class StringExtensionsTests
{
    #region ContainsIgnoreCase Tests
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenSourceContainsValueWithSameCase_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenSourceContainsValueWithDifferentCase_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenSourceDoesNotContainValue_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenSourceIsNull_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenValueIsNull_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenBothAreNull_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenValueIsEmpty_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the ContainsIgnoreCase_WhenSourceIsEmpty_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenValuesAreEqualWithSameCase_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenValuesAreEqualWithDifferentCase_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenValuesAreNotEqual_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenSourceIsNull_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenValueIsNull_ReturnsFalse scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenBothAreNull_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenBothAreEmpty_ReturnsTrue scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the EqualsIgnoreCase_WhenSourceIsEmptyAndValueIsNot_ReturnsFalse scenario.
    /// </summary>

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
