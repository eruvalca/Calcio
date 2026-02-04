namespace Calcio.Shared.Validation;

/// <summary>
/// Defines column name aliases for player import file parsing.
/// Supports auto-detection of columns by matching header names to known aliases.
/// </summary>
public static class PlayerImportColumnMapping
{
    /// <summary>
    /// Required fields that must be present in the import file.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> RequiredFields { get; } = new Dictionary<string, string[]>
    {
        ["FirstName"] =
        [
            "first_name", "firstname", "first", "player_first_name",
            "given_name", "givenname", "player first name", "first name"
        ],
        ["LastName"] =
        [
            "last_name", "lastname", "last", "player_last_name",
            "surname", "family_name", "familyname", "player last name", "last name"
        ],
        ["DateOfBirth"] =
        [
            "date_of_birth", "dateofbirth", "dob", "birth_date", "birthdate",
            "birthday", "birth date", "date of birth"
        ],
        ["Gender"] =
        [
            "gender", "sex", "player_gender"
        ]
    };

    /// <summary>
    /// Optional fields that can be imported if present.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> OptionalFields { get; } = new Dictionary<string, string[]>
    {
        ["GraduationYear"] =
        [
            "graduation_year", "graduationyear", "grad_year", "gradyear",
            "grad", "graduation", "class_of", "classof", "graduation year", "grad year"
        ],
        ["JerseyNumber"] =
        [
            "jersey_number", "jerseynumber", "jersey", "number", "jersey_no",
            "jersey #", "jersey#", "jersey number", "player_number"
        ],
        ["TryoutNumber"] =
        [
            "tryout_number", "tryoutnumber", "tryout", "tryout_no",
            "tryout #", "tryout#", "tryout number", "evaluation_number"
        ]
    };

    /// <summary>
    /// All fields combined for lookup.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> AllFields { get; } =
        RequiredFields.Concat(OptionalFields).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    /// <summary>
    /// Attempts to find a matching field name for a given column header.
    /// </summary>
    /// <param name="columnHeader">The column header from the import file.</param>
    /// <returns>The canonical field name if matched, or null if no match found.</returns>
    public static string? FindMatchingField(string columnHeader)
    {
        if (string.IsNullOrWhiteSpace(columnHeader))
        {
            return null;
        }

        var normalizedHeader = columnHeader.Trim().ToLowerInvariant();

        foreach (var (fieldName, aliases) in AllFields)
        {
            if (aliases.Any(alias => alias.Equals(normalizedHeader, StringComparison.OrdinalIgnoreCase)))
            {
                return fieldName;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the template headers in the preferred order for generating templates.
    /// </summary>
    public static IReadOnlyList<string> TemplateHeaders { get; } =
    [
        "first_name",
        "last_name",
        "date_of_birth",
        "gender",
        "graduation_year",
        "jersey_number",
        "tryout_number"
    ];

    /// <summary>
    /// Gets the display-friendly headers for template generation.
    /// </summary>
    public static IReadOnlyList<string> TemplateDisplayHeaders { get; } =
    [
        "First Name",
        "Last Name",
        "Date of Birth",
        "Gender",
        "Graduation Year",
        "Jersey Number",
        "Tryout Number"
    ];
}
