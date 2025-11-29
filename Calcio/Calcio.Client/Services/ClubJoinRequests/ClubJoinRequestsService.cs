using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using OneOf.Types;

namespace Calcio.Client.Services.ClubJoinRequests;

public class ClubJoinRequestsService(HttpClient httpClient) : IClubJoinRequestsService
{
    public async Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/club-join-requests/current", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ClubJoinRequestDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null");
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/club-join-requests/{clubId}", null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync("api/club-join-requests/current", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/join-requests", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ClubJoinRequestWithUserDto>>(cancellationToken) ?? [];
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<Success>> ApproveJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/clubs/{clubId}/join-requests/{requestId}/approve", null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<Success>> RejectJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/clubs/{clubId}/join-requests/{requestId}/reject", null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }
}
