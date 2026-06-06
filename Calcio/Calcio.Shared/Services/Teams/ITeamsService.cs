using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Teams;

/// <summary>
/// Defines team-management operations for club contexts.
/// </summary>
public interface ITeamsService
{
    /// <summary>
    /// Gets all teams for a specific club.
    /// </summary>
    /// <param name="clubId">The unique identifier of the club.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result containing the club teams, or a problem describing why retrieval failed.</returns>
    Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new team for a specific club.
    /// </summary>
    /// <param name="clubId">The unique identifier of the club.</param>
    /// <param name="dto">The team details to create.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result when creation succeeds; otherwise a problem describing the failure.</returns>
    Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken);
}
