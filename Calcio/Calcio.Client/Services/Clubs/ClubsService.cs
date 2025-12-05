using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Clubs;

namespace Calcio.Client.Services.Clubs;

public class ClubsService(HttpClient httpClient) : IClubsService
{
    public async Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/clubs", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? [];
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<List<BaseClubDto>>> GetAllClubsForBrowsingAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/clubs/all", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? [];
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<ClubCreatedDto>> CreateClubAsync(CreateClubDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("api/clubs", dto, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ClubCreatedDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null");
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }
}
