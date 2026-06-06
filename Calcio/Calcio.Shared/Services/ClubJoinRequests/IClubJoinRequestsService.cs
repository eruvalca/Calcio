using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.ClubJoinRequests;

/// <summary>
/// Defines join-request operations for the current user and club administrators.
/// </summary>
public interface IClubJoinRequestsService
{
    /// <summary>
    /// Gets the current user's join request, if one exists.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A successful result containing the current user's request, or a problem when the request cannot be retrieved.
    /// </returns>
    Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a join request for the current user against the specified club.
    /// </summary>
    /// <param name="clubId">The club to request membership for.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result when the request is created; otherwise a problem describing the failure.</returns>
    Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels the current user's active join request.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result when cancellation succeeds; otherwise a problem describing the failure.</returns>
    Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets pending join requests for a specific club for administrative review.
    /// </summary>
    /// <param name="clubId">The club whose pending requests should be returned.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A successful result containing pending requests with user information, or a problem describing why retrieval failed.
    /// </returns>
    Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the status of a specific join request for a club.
    /// </summary>
    /// <param name="clubId">The club that owns the join request.</param>
    /// <param name="requestId">The unique identifier of the join request.</param>
    /// <param name="status">The new status to apply.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A successful result when the update succeeds; otherwise a problem describing the failure.</returns>
    Task<ServiceResult<Success>> UpdateJoinRequestStatusAsync(long clubId, long requestId, RequestStatus status, CancellationToken cancellationToken);
}
