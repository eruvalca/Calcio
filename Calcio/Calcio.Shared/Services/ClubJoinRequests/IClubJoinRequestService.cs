using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.ClubJoinRequests;

public interface IClubJoinRequestService
{
    Task<OneOf<ClubJoinRequestDto, NotFound, Unauthorized, Error>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Conflict, Unauthorized, Error>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Unauthorized, Error>> CancelJoinRequestAsync(CancellationToken cancellationToken);
    Task<OneOf<List<ClubJoinRequestWithUserDto>, Unauthorized, Error>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Unauthorized, Error>> ApproveJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Unauthorized, Error>> RejectJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken);
}
