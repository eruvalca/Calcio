using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Entities;

namespace Calcio.Shared.Extensions.ClubJoinRequests;

public static class ClubJoinRequestEntityExtensions
{
    extension(ClubJoinRequestEntity request)
    {
        public ClubJoinRequestDto ToClubJoinRequestDto()
            => new(request.ClubJoinRequestId, request.ClubId, request.RequestingUserId, request.Status);

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
