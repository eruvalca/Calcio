using System.Net.Http.Json;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using OneOf.Types;

namespace Calcio.Client.Services.ClubJoinRequests;

/// <summary>
/// Handles club join request workflows for current users and club administrators.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class ClubJoinRequestsService(HttpClient httpClient) : IClubJoinRequestsService
{
    /// <summary>
    /// Retrieves the pending join request created by the current user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// The current user's join request when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubJoinRequests.GetCurrent, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ClubJoinRequestDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a join request for the specified club on behalf of the current user.
    /// </summary>
    /// <param name="clubId">Identifier of the target club.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when the join request is created; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(Routes.ClubJoinRequests.ForClub(clubId), null, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Cancels the current user's active join request.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when cancellation succeeds; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubJoinRequests.CancelCurrent, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves pending join requests awaiting review for a specific club.
    /// </summary>
    /// <param name="clubId">Identifier of the club being administered.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of pending requests when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubJoinRequests.Admin.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ClubJoinRequestWithUserDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the review status of a specific join request for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club that owns the request.</param>
    /// <param name="requestId">Identifier of the join request to update.</param>
    /// <param name="status">New status assigned by the reviewer.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when the status is updated; otherwise a mapped service problem.
    /// </returns>
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
