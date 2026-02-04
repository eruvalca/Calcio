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

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ClubJoinRequestDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(Routes.ClubJoinRequests.ForClub(clubId), null, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubJoinRequests.CancelCurrent, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubJoinRequests.Admin.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ClubJoinRequestWithUserDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
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

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
