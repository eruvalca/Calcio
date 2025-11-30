using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.Validation;

/// <summary>
/// Validates that a DateOnly value is today or later.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DateNotBeforeTodayAttribute : ValidationAttribute
{
    public DateNotBeforeTodayAttribute()
        : base("The {0} must be today or later.")
    {
    }

    public override bool IsValid(object? value)
        => value switch
        {
            null => true,
            DateOnly date => date >= DateOnly.FromDateTime(DateTime.Today),
            _ => false
        };
}
