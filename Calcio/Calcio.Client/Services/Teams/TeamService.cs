using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.Teams;

public class TeamService(HttpClient httpClient) : ITeamService
{
    public async Task<OneOf<List<TeamDto>, Unauthorized, Error>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/teams", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<TeamDto>>(cancellationToken) ?? [],
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }
}
