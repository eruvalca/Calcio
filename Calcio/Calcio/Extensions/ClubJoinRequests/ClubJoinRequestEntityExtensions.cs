using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Entities;

namespace Calcio.Extensions.ClubJoinRequests;

/// <summary>
/// Provides extension members for Club Join Request Entity Extensions.
/// </summary>
public static class ClubJoinRequestEntityExtensions
{
    extension(ClubJoinRequestEntity request)
    {
        /// <summary>
        /// Executes the To Club Join Request Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public ClubJoinRequestDto ToClubJoinRequestDto()
            => new(request.ClubJoinRequestId, request.ClubId, request.RequestingUserId, request.Status);

        /// <summary>
        /// Executes the To Club Join Request With User Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public ClubJoinRequestWithUserDto ToClubJoinRequestWithUserDto()
            => new(
                request.ClubJoinRequestId,
                request.ClubId,
                request.RequestingUserId,
                request.RequestingUser.FullName,
                request.RequestingUser.Email ?? string.Empty,
                request.Status,
                request.CreatedAt);
    }
}
