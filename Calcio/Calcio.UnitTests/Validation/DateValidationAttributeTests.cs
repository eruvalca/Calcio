using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

using Shouldly;

namespace Calcio.UnitTests.Validation;

public class DateValidationAttributeTests
{
    #region DateNotBeforeTodayAttribute Tests

    [Fact]
    public void DateNotBeforeToday_WhenNull_ReturnsValid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateNotBeforeToday_WhenToday_ReturnsValid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var result = attribute.IsValid(today);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateNotBeforeToday_WhenTomorrow_ReturnsValid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        // Act
        var result = attribute.IsValid(tomorrow);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateNotBeforeToday_WhenYesterday_ReturnsInvalid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // Act
        var result = attribute.IsValid(yesterday);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DateNotBeforeToday_WhenFutureDate_ReturnsValid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

        // Act
        var result = attribute.IsValid(futureDate);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateNotBeforeToday_WhenInvalidType_ReturnsInvalid()
    {
        // Arrange
        var attribute = new DateNotBeforeTodayAttribute();

        // Act
        var result = attribute.IsValid("not a date");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DateAfterTodayAttribute Tests

    [Fact]
    public void DateAfterToday_WhenNull_ReturnsValid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateAfterToday_WhenToday_ReturnsInvalid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var result = attribute.IsValid(today);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DateAfterToday_WhenTomorrow_ReturnsValid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        // Act
        var result = attribute.IsValid(tomorrow);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateAfterToday_WhenYesterday_ReturnsInvalid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // Act
        var result = attribute.IsValid(yesterday);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DateAfterToday_WhenFutureDate_ReturnsValid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

        // Act
        var result = attribute.IsValid(futureDate);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DateAfterToday_WhenInvalidType_ReturnsInvalid()
    {
        // Arrange
        var attribute = new DateAfterTodayAttribute();

        // Act
        var result = attribute.IsValid("not a date");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DateAfterAttribute Tests

    [Fact]
    public void DateAfter_WhenNull_ReturnsValid()
    {
        // Arrange
        var model = new TestDateModel { StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModel.EndDate) };
        var attribute = new DateAfterAttribute(nameof(TestDateModel.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void DateAfter_WhenEndDateAfterStartDate_ReturnsValid()
    {
        // Arrange
        var model = new TestDateModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModel.EndDate) };
        var attribute = new DateAfterAttribute(nameof(TestDateModel.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void DateAfter_WhenEndDateEqualsStartDate_ReturnsInvalid()
    {
        // Arrange
        var sameDate = DateOnly.FromDateTime(DateTime.Today);
        var model = new TestDateModel { StartDate = sameDate, EndDate = sameDate };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModel.EndDate) };
        var attribute = new DateAfterAttribute(nameof(TestDateModel.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("must be after");
    }

    [Fact]
    public void DateAfter_WhenEndDateBeforeStartDate_ReturnsInvalid()
    {
        // Arrange
        var model = new TestDateModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModel.EndDate) };
        var attribute = new DateAfterAttribute(nameof(TestDateModel.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("must be after");
    }

    [Fact]
    public void DateAfter_WhenOtherPropertyNotFound_ReturnsError()
    {
        // Arrange
        var model = new TestDateModel { StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModel.EndDate) };
        var attribute = new DateAfterAttribute("NonExistentProperty");

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Unknown property");
    }

    [Fact]
    public void DateAfter_WhenOtherPropertyIsNull_ReturnsValid()
    {
        // Arrange
        var model = new TestDateModelWithNullableStart
        {
            StartDate = null,
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModelWithNullableStart.EndDate) };
        var attribute = new DateAfterAttribute(nameof(TestDateModelWithNullableStart.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void DateAfter_UsesDisplayNameInErrorMessage()
    {
        // Arrange
        var model = new TestDateModelWithDisplayNames
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestDateModelWithDisplayNames.EndDate), DisplayName = "Season End Date" };
        var attribute = new DateAfterAttribute(nameof(TestDateModelWithDisplayNames.StartDate));

        // Act
        var result = attribute.GetValidationResult(model.EndDate, context);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Season End Date");
        result.ErrorMessage.ShouldContain("Season Start Date");
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestDateModel
    {
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    private sealed class TestDateModelWithNullableStart
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    private sealed class TestDateModelWithDisplayNames
    {
        [Display(Name = "Season Start Date")]
        public DateOnly StartDate { get; set; }

        [Display(Name = "Season End Date")]
        public DateOnly? EndDate { get; set; }
    }

    #endregion
}
