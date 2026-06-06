namespace Calcio.Shared.DTOs.Players.BulkImport;

/// <summary>
/// Represents the result of attempting to map a player field to a column in the import file.
/// </summary>
/// <param name="FieldName">The canonical field name (e.g., "FirstName", "DateOfBirth").</param>
/// <param name="DetectedColumnName">The column header name that was matched, or null if not found.</param>
/// <param name="IsRequired">Whether this field is required for a valid import.</param>
/// <param name="IsDetected">Whether a matching column was found in the file.</param>
public sealed record ColumnMappingResultDto(
    string FieldName,
    string? DetectedColumnName,
    bool IsRequired,
    bool IsDetected);
