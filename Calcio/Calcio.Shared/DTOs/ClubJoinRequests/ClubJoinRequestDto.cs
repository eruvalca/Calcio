using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.ClubJoinRequests;

public record ClubJoinRequestDto(
    long ClubJoinRequestId,
    long ClubId,
    long RequestingUserId,
    RequestStatus Status);
