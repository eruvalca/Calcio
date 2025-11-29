using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.Players;

public class PlayersService(HttpClient httpClient) : IPlayersService
{
    public async Task<OneOf<List<ClubPlayerDto>, Unauthorized, Error>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/players", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<ClubPlayerDto>>(cancellationToken) ?? [],
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }
}
