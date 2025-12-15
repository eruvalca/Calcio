using System.Net;
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
            ? (ServiceResult<List<TeamDto>>)(await response.Content.ReadFromJsonAsync<List<TeamDto>>(cancellationToken) ?? [])
            : (ServiceResult<List<TeamDto>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Teams.ForClub(clubId), dto, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(),
            _ => ServiceProblem.ServerError()
        };
    }
}
