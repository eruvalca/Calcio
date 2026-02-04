using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Results;

namespace Calcio.Shared.Services.Players;

/// <summary>
/// Service for parsing and validating player import files.
/// </summary>
public interface IPlayerImportParserService
{
    /// <summary>
    /// Parses and validates a player import file (CSV or Excel).
    /// </summary>
    /// <param name="fileStream">The file stream to parse.</param>
    /// <param name="fileName">The file name (used to detect format from extension).</param>
    /// <param name="clubId">The club ID for checking duplicates against existing players.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result with all parsed rows and column mappings.</returns>
    Task<ServiceResult<BulkValidateResultDto>> ParseAndValidateAsync(
        Stream fileStream,
        string fileName,
        long clubId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-validates a list of import rows, checking for duplicates against the database.
    /// Used after user edits rows in the preview grid.
    /// </summary>
    /// <param name="rows">The rows to re-validate.</param>
    /// <param name="clubId">The club ID for checking duplicates against existing players.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The re-validated rows.</returns>
    Task<ServiceResult<BulkValidateResultDto>> RevalidateRowsAsync(
        List<PlayerImportRowDto> rows,
        long clubId,
        CancellationToken cancellationToken);
}
