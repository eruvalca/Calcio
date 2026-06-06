using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.Players;

/// <summary>
/// Service for managing player data and photos within a club.
/// </summary>
public interface IPlayersService
{
    /// <summary>
    /// Gets all players for a club, ordered by last name then first name.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of players belonging to the club.</returns>
    Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new player in the specified club.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="dto">The player creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created player information.</returns>
    Task<ServiceResult<PlayerCreatedDto>> CreatePlayerAsync(long clubId, CreatePlayerDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a photo for a player, replacing any existing photo.
    /// The photo is processed into multiple size variants (small, medium, large).
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="photoStream">The photo data stream.</param>
    /// <param name="contentType">The MIME content type of the photo.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uploaded photo information with SAS URLs for all variants.</returns>
    Task<ServiceResult<PlayerPhotoDto>> UploadPlayerPhotoAsync(
        long clubId,
        long playerId,
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the photo for a player if one exists.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The photo information with SAS URLs, or None if no photo exists.</returns>
    Task<ServiceResult<OneOf<PlayerPhotoDto, None>>> GetPlayerPhotoAsync(long clubId, long playerId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a player import file (CSV or Excel) and returns parsed rows with validation results.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="fileStream">The file stream to validate.</param>
    /// <param name="fileName">The file name (used to detect format).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result with parsed rows and column mappings.</returns>
    Task<ServiceResult<BulkValidateResultDto>> ValidateBulkImportAsync(
        long clubId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-validates a list of import rows after user edits, checking for duplicates against the database.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="rows">The rows to re-validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The re-validated rows.</returns>
    Task<ServiceResult<BulkValidateResultDto>> RevalidateBulkImportAsync(
        long clubId,
        List<PlayerImportRowDto> rows,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates multiple players from validated import rows.
    /// Only rows marked for import (IsMarkedForImport=true) and valid (IsValid=true) will be created.
    /// </summary>
    /// <param name="clubId">The club identifier.</param>
    /// <param name="rows">The validated import rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result with created and skipped counts.</returns>
    Task<ServiceResult<BulkImportResultDto>> BulkCreatePlayersAsync(
        long clubId,
        List<PlayerImportRowDto> rows,
        CancellationToken cancellationToken);
}
