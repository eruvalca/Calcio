using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.Validation;

/// <summary>
/// Validates that a DateOnly value is strictly after another property's DateOnly value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DateAfterAttribute(string otherPropertyName) : ValidationAttribute("The {0} must be after {1}.")
{
    public string OtherPropertyName { get; } = otherPropertyName;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not DateOnly thisDate)
        {
            return new ValidationResult("Invalid date value.");
        }

        var otherProperty = validationContext.ObjectType.GetProperty(OtherPropertyName);
        if (otherProperty is null)
        {
            return new ValidationResult($"Unknown property: {OtherPropertyName}");
        }

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance);
        if (otherValue is not DateOnly otherDate)
        {
            return ValidationResult.Success;
        }

        if (thisDate <= otherDate)
        {
            var displayName = validationContext.DisplayName;
            var otherDisplayAttr = otherProperty.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();
            var otherDisplayName = otherDisplayAttr?.Name ?? OtherPropertyName;

            return new ValidationResult(
                $"The {displayName} must be after {otherDisplayName}.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
