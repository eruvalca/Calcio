namespace Calcio.Shared.DTOs.Players.BulkImport;

/// <summary>
/// The result of executing a bulk player import.
/// </summary>
/// <param name="CreatedCount">Number of players successfully created.</param>
/// <param name="SkippedCount">Number of rows that were skipped (invalid or unmarked).</param>
public sealed record BulkImportResultDto(
    int CreatedCount,
    int SkippedCount);
