using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

namespace Calcio.Client.Services.Teams;

public class TeamsService(HttpClient httpClient) : ITeamsService
{
    public async Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/teams", cancellationToken);

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
}
