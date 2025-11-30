using Calcio.Shared.Validation;

using Shouldly;

namespace Calcio.UnitTests.Validation;

public class GraduationYearAttributeTests
{
    private static int CurrentYear => DateTime.Today.Year;
    private static int MaxYear => CurrentYear + 25;

    #region GraduationYearAttribute Tests

    [Fact]
    public void GraduationYear_WhenNull_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_WhenCurrentYear_ReturnsValid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(CurrentYear);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GraduationYear_WhenNextYear_ReturnsValid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(CurrentYear + 1);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GraduationYear_WhenLastYear_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(CurrentYear - 1);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_WhenMaxYear_ReturnsValid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(MaxYear);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GraduationYear_WhenAboveMaxYear_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(MaxYear + 1);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_WhenYearWithinRange_ReturnsValid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(CurrentYear + 5);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GraduationYear_WhenNegativeYear_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(-2025);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_WhenZero_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid(0);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_WhenInvalidType_ReturnsInvalid()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();

        // Act
        var result = attribute.IsValid("not a year");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GraduationYear_FormatErrorMessage_ContainsMinAndMaxYear()
    {
        // Arrange
        var attribute = new GraduationYearAttribute();
        const string fieldName = "Graduation Year";

        // Act
        var errorMessage = attribute.FormatErrorMessage(fieldName);

        // Assert
        errorMessage.ShouldContain(fieldName);
        errorMessage.ShouldContain(CurrentYear.ToString());
        errorMessage.ShouldContain(MaxYear.ToString());
    }

    #endregion
}
