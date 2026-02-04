using Calcio.Shared.Extensions;

using Shouldly;

namespace Calcio.UnitTests.Extensions;

public class GraduationYearCalculatorTests
{
    [Fact]
    public void ComputeFromDateOfBirth_WhenBornInJanuary_Returns18YearsFromBirthYear()
    {
        // Arrange
        var dob = new DateOnly(2010, 1, 15);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // Born January 2010 -> turns 18 in 2028 before August -> graduates 2028
        result.ShouldBe(2028);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornInJuly_Returns18YearsFromBirthYear()
    {
        // Arrange
        var dob = new DateOnly(2010, 7, 31);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // Born July 2010 -> turns 18 in 2028 before August -> graduates 2028
        result.ShouldBe(2028);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornInAugust_Returns19YearsFromBirthYear()
    {
        // Arrange
        var dob = new DateOnly(2010, 8, 1);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // Born August 2010 -> turns 18 in August 2028 (after cutoff) -> graduates 2029
        result.ShouldBe(2029);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornInDecember_Returns19YearsFromBirthYear()
    {
        // Arrange
        var dob = new DateOnly(2010, 12, 25);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // Born December 2010 -> turns 18 in December 2028 (after cutoff) -> graduates 2029
        result.ShouldBe(2029);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornOnAugustCutoff_Returns19YearsFromBirthYear()
    {
        // Arrange - born exactly on August cutoff
        var dob = new DateOnly(2005, 8, 1);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // August birthdays are after the cutoff -> +19 years from birth year
        result.ShouldBe(2024);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornInSeptember_Returns19YearsFromBirthYear()
    {
        // Arrange
        var dob = new DateOnly(2005, 9, 15);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        result.ShouldBe(2024);
    }

    [Fact]
    public void ComputeFromDateOfBirth_WhenBornOnLastDayOfJuly_Returns18YearsFromBirthYear()
    {
        // Arrange - born on last day before August cutoff
        var dob = new DateOnly(2005, 7, 31);

        // Act
        var result = GraduationYearCalculator.ComputeFromDateOfBirth(dob);

        // Assert
        // July is before the cutoff -> +18 years from birth year
        result.ShouldBe(2023);
    }
}
