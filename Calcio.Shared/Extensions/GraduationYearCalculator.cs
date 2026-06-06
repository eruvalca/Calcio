namespace Calcio.Shared.Extensions;

/// <summary>
/// Provides methods for computing graduation year from date of birth.
/// </summary>
public static class GraduationYearCalculator
{
    /// <summary>
    /// The month (August) that typically marks the school year cutoff.
    /// Children born in August or later typically start school the following year.
    /// </summary>
    private const int SchoolYearCutoffMonth = 8;

    /// <summary>
    /// The typical age at which students graduate high school (18 years old).
    /// </summary>
    private const int TypicalGraduationAge = 18;

    /// <summary>
    /// Computes the expected graduation year based on date of birth.
    /// Uses a typical US school system assumption where:
    /// - Students graduate at age 18
    /// - Students born August-December start school the following year
    ///   (and thus graduate one year later than students born earlier in the same calendar year)
    /// </summary>
    /// <param name="dateOfBirth">The player's date of birth.</param>
    /// <returns>The computed graduation year.</returns>
    /// <example>
    /// Born January 2010 → Graduates 2028 (2010 + 18)
    /// Born September 2010 → Graduates 2029 (2010 + 18 + 1 for fall cutoff)
    /// </example>
    public static int ComputeFromDateOfBirth(DateOnly dateOfBirth)
    {
        var baseGraduationYear = dateOfBirth.Year + TypicalGraduationAge;

        // If born in August or later, they typically start school the following year
        // and thus graduate one year later
        if (dateOfBirth.Month >= SchoolYearCutoffMonth)
        {
            return baseGraduationYear + 1;
        }

        return baseGraduationYear;
    }
}
