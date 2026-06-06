using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

using Shouldly;

namespace Calcio.UnitTests.Validation;

/// <summary>
/// Contains unit tests for D at eV al id at io nA tt ri bu te behavior.
/// </summary>
public class DateValidationAttributeTests
{
    #region DateNotBeforeTodayAttribute Tests
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenNull_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenToday_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenTomorrow_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenYesterday_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenFutureDate_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateNotBeforeToday_WhenInvalidType_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenNull_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenToday_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenTomorrow_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenYesterday_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenFutureDate_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfterToday_WhenInvalidType_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenNull_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenEndDateAfterStartDate_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenEndDateEqualsStartDate_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenEndDateBeforeStartDate_ReturnsInvalid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenOtherPropertyNotFound_ReturnsError scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_WhenOtherPropertyIsNull_ReturnsValid scenario.
    /// </summary>

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
    /// <summary>
    /// Verifies the DateAfter_UsesDisplayNameInErrorMessage scenario.
    /// </summary>

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

    /// <summary>
    /// Contains unit tests for T es tD at eM od el behavior.
    /// </summary>
    private sealed class TestDateModel
    {
        /// <summary>
        /// Gets or sets the s ta rt da te value used by validation test models.
        /// </summary>
        public DateOnly StartDate { get; set; }
        /// <summary>
        /// Gets or sets the e nd da te value used by validation test models.
        /// </summary>
        public DateOnly? EndDate { get; set; }
    }

    /// <summary>
    /// Contains unit tests for T es tD at eM od el Wi th Nu ll ab le St ar t behavior.
    /// </summary>
    private sealed class TestDateModelWithNullableStart
    {
        /// <summary>
        /// Gets or sets the s ta rt da te value used by validation test models.
        /// </summary>
        public DateOnly? StartDate { get; set; }
        /// <summary>
        /// Gets or sets the e nd da te value used by validation test models.
        /// </summary>
        public DateOnly? EndDate { get; set; }
    }

    /// <summary>
    /// Contains unit tests for T es tD at eM od el Wi th Di sp la yN am es behavior.
    /// </summary>
    private sealed class TestDateModelWithDisplayNames
    {
        [Display(Name = "Season Start Date")]
        /// <summary>
        /// Gets or sets the s ta rt da te value used by validation test models.
        /// </summary>
        public DateOnly StartDate { get; set; }

        [Display(Name = "Season End Date")]
        /// <summary>
        /// Gets or sets the e nd da te value used by validation test models.
        /// </summary>
        public DateOnly? EndDate { get; set; }
    }

    #endregion
}
