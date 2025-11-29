using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

namespace Calcio.Client.Services.Players;

public class PlayersService(HttpClient httpClient) : IPlayersService
{
    public async Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/players", cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<List<ClubPlayerDto>>)(await response.Content.ReadFromJsonAsync<List<ClubPlayerDto>>(cancellationToken) ?? [])
            : (ServiceResult<List<ClubPlayerDto>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                _ => ServiceProblem.ServerError()
            });
    }
}
