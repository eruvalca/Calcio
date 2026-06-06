using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Seasons;

/// <summary>
/// Defines season-management operations for club contexts.
/// </summary>
public interface ISeasonsService
{
    /// <summary>
    /// Gets all seasons for a specific club.
    /// </summary>
    /// <param name="clubId">The unique identifier of the club.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result containing the club seasons, or a problem describing why retrieval failed.</returns>
    Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new season for a specific club.
    /// </summary>
    /// <param name="clubId">The unique identifier of the club.</param>
    /// <param name="dto">The season details to create.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result when creation succeeds; otherwise a problem describing the failure.</returns>
    Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken);
}
