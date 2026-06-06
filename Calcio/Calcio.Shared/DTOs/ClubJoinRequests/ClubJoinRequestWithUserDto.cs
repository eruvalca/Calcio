using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.ClubJoinRequests;

/// <summary>
/// Represents a club join request enriched with the requesting user's details.
/// </summary>
/// <param name="ClubJoinRequestId">The unique identifier of the join request.</param>
/// <param name="ClubId">The club the user is requesting to join.</param>
/// <param name="RequestingUserId">The unique identifier of the requesting user.</param>
/// <param name="RequestingUserFullName">The full name of the requesting user.</param>
/// <param name="RequestingUserEmail">The email address of the requesting user.</param>
/// <param name="Status">The current approval state of the request.</param>
/// <param name="RequestedAt">The UTC timestamp when the request was created.</param>
public record ClubJoinRequestWithUserDto(
    long ClubJoinRequestId,
    long ClubId,
    long RequestingUserId,
    string RequestingUserFullName,
    string RequestingUserEmail,
    RequestStatus Status,
    DateTimeOffset RequestedAt);
