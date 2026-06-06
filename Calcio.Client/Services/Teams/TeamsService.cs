using System.Net.Http.Json;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using OneOf.Types;

namespace Calcio.Client.Services.Teams;

/// <summary>
/// Provides client-side team management operations for clubs.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class TeamsService(HttpClient httpClient) : ITeamsService
{
    /// <summary>
    /// Retrieves teams that belong to a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club whose teams are requested.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of teams when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Teams.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<TeamDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new team for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club where the team is created.</param>
    /// <param name="dto">Payload describing the team to create.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when creation succeeds; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Teams.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
