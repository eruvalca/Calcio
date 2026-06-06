using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.Validation;

/// <summary>
/// Validates that an integer value represents a valid graduation year.
/// The year must be the current year or later, up to 25 years in the future.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class GraduationYearAttribute : ValidationAttribute
{
    private const int MaxYearsInFuture = 25;

    public GraduationYearAttribute()
        : base("The {0} must be between {1} and {2}.")
    {
    }

    public override bool IsValid(object? value)
        => value switch
        {
            int year => year >= MinYear && year <= MaxYear,
            _ => false
        };

    public override string FormatErrorMessage(string name)
        => string.Format(ErrorMessageString, name, MinYear, MaxYear);

    private static int MinYear => DateTime.Today.Year;
    private static int MaxYear => DateTime.Today.Year + MaxYearsInFuture;
}
