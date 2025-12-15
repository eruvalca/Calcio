using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using OneOf.Types;

namespace Calcio.Client.Services.ClubJoinRequests;

public class ClubJoinRequestsService(HttpClient httpClient) : IClubJoinRequestsService
{
    public async Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubJoinRequests.GetCurrent, cancellationToken);

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
        var response = await httpClient.PostAsync(Routes.ClubJoinRequests.ForClub(clubId), null, cancellationToken);

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
        var response = await httpClient.DeleteAsync(Routes.ClubJoinRequests.CancelCurrent, cancellationToken);

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
        var response = await httpClient.GetAsync(Routes.ClubJoinRequests.Admin.ForClub(clubId), cancellationToken);

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

    public async Task<ServiceResult<Success>> UpdateJoinRequestStatusAsync(
        long clubId,
        long requestId,
        RequestStatus status,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsJsonAsync(
            Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId),
            new UpdateClubJoinRequestStatusDto(status),
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            HttpStatusCode.BadRequest => ServiceProblem.BadRequest(),
            _ => ServiceProblem.ServerError()
        };
    }
}
