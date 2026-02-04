namespace Calcio.Shared.DTOs.Players.BulkImport;

/// <summary>
/// The result of validating a player import file.
/// </summary>
/// <param name="Rows">All parsed rows with their validation results.</param>
/// <param name="ColumnMappings">The column mapping results showing which fields were detected.</param>
/// <param name="MissingRequiredColumns">List of required column names that were not found in the file.</param>
/// <param name="ValidCount">Number of rows that passed validation.</param>
/// <param name="ErrorCount">Number of rows with validation errors.</param>
/// <param name="WarningCount">Number of rows with warnings (but still valid).</param>
/// <param name="DuplicateInFileCount">Number of rows that are duplicates within the file.</param>
/// <param name="DuplicateInDatabaseCount">Number of rows that match existing players in the database.</param>
public sealed record BulkValidateResultDto(
    List<PlayerImportRowDto> Rows,
    List<ColumnMappingResultDto> ColumnMappings,
    List<string> MissingRequiredColumns,
    int ValidCount,
    int ErrorCount,
    int WarningCount,
    int DuplicateInFileCount,
    int DuplicateInDatabaseCount);
