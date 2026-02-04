using System.Net.Http.Json;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using OneOf.Types;

namespace Calcio.Client.Services.Seasons;

public class SeasonsService(HttpClient httpClient) : ISeasonsService
{
    public async Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Seasons.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<SeasonDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Seasons.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
