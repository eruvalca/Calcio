using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.Players.BulkImport;

/// <summary>
/// Represents a single row from a player import file with validation results.
/// </summary>
public sealed class PlayerImportRowDto
{
    /// <summary>
    /// Unique identifier for this row instance, used for stable UI keying.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The 1-based row number from the source file.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Player's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Player's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Player's date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Player's gender.
    /// </summary>
    public Gender? Gender { get; set; }

    /// <summary>
    /// Player's graduation year.
    /// </summary>
    public int? GraduationYear { get; set; }

    /// <summary>
    /// Player's jersey number.
    /// </summary>
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Player's tryout number.
    /// </summary>
    public int? TryoutNumber { get; set; }

    /// <summary>
    /// List of validation errors that prevent import.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// List of warnings that don't prevent import but should be reviewed.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Whether the row passed validation and can be imported.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Whether the graduation year was computed from date of birth.
    /// </summary>
    public bool IsGraduationYearComputed { get; set; }

    /// <summary>
    /// Whether the row is marked for import. Defaults to true for valid rows.
    /// </summary>
    public bool IsMarkedForImport { get; set; } = true;

    /// <summary>
    /// Whether this row is a potential duplicate of another row in the import.
    /// </summary>
    public bool IsDuplicateInFile { get; set; }

    /// <summary>
    /// Whether this row is a potential duplicate of an existing player in the database.
    /// </summary>
    public bool IsDuplicateInDatabase { get; set; }
}
