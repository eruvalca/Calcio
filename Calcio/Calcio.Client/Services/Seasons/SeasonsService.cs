using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.Seasons;

public class SeasonsService(HttpClient httpClient) : ISeasonsService
{
    public async Task<OneOf<List<SeasonDto>, Unauthorized, Error>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/seasons", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<SeasonDto>>(cancellationToken) ?? [],
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }
}
