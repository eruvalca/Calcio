using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.ClubJoinRequests;

/// <summary>
/// Represents the requested status update for an existing club join request.
/// </summary>
/// <param name="Status">The new status to apply to the join request.</param>
public record UpdateClubJoinRequestStatusDto(RequestStatus Status);
