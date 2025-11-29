using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

namespace Calcio.Client.Services.Seasons;

public class SeasonsService(HttpClient httpClient) : ISeasonsService
{
    public async Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/seasons", cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<List<SeasonDto>>)(await response.Content.ReadFromJsonAsync<List<SeasonDto>>(cancellationToken) ?? [])
            : (ServiceResult<List<SeasonDto>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                _ => ServiceProblem.ServerError()
            });
    }
}
