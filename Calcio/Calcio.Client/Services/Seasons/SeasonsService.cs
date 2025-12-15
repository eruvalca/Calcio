using System.Net;
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
            ? (ServiceResult<List<SeasonDto>>)(await response.Content.ReadFromJsonAsync<List<SeasonDto>>(cancellationToken) ?? [])
            : (ServiceResult<List<SeasonDto>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Seasons.ForClub(clubId), dto, cancellationToken);

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
