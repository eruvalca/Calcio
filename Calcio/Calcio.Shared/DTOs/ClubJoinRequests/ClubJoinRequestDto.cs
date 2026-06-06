using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.ClubJoinRequests;

/// <summary>
/// Represents the core data for a club join request.
/// </summary>
/// <param name="ClubJoinRequestId">The unique identifier of the join request.</param>
/// <param name="ClubId">The club the user is requesting to join.</param>
/// <param name="RequestingUserId">The user who submitted the request.</param>
/// <param name="Status">The current status of the join request.</param>
public record ClubJoinRequestDto(
    long ClubJoinRequestId,
    long ClubId,
    long RequestingUserId,
    RequestStatus Status);
