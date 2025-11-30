using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.Validation;

/// <summary>
/// Validates that a DateOnly value is strictly after today.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DateAfterTodayAttribute : ValidationAttribute
{
    public DateAfterTodayAttribute()
        : base("The {0} must be after today.")
    {
    }

    public override bool IsValid(object? value)
        => value switch
        {
            null => true,
            DateOnly date => date > DateOnly.FromDateTime(DateTime.Today),
            _ => false
        };
}
