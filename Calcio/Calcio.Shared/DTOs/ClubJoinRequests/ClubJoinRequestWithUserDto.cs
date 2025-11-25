using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.ClubJoinRequests;

public record ClubJoinRequestWithUserDto(
    long ClubJoinRequestId,
    long ClubId,
    long RequestingUserId,
    string RequestingUserFullName,
    string RequestingUserEmail,
    RequestStatus Status,
    DateTimeOffset RequestedAt);
