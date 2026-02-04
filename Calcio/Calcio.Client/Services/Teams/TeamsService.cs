using System.Net.Http.Json;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using OneOf.Types;

namespace Calcio.Client.Services.Teams;

public class TeamsService(HttpClient httpClient) : ITeamsService
{
    public async Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Teams.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<TeamDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Teams.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
