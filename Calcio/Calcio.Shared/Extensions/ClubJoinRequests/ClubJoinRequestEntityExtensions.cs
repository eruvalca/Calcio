using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Models.Entities;

namespace Calcio.Shared.Extensions.ClubJoinRequests;

public static class ClubJoinRequestEntityExtensions
{
    extension(ClubJoinRequestEntity request)
    {
        public ClubJoinRequestDto ToClubJoinRequestDto()
            => new(request.ClubJoinRequestId, request.ClubId, request.RequestingUserId, request.Status);
    }
}
