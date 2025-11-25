using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.ClubJoinRequests;

public interface IClubJoinRequestService
{
    Task<OneOf<ClubJoinRequestDto, NotFound, Unauthorized, Error>> GetPendingRequestForCurrentUserAsync(CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Conflict, Unauthorized, Error>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Unauthorized, Error>> CancelJoinRequestAsync(CancellationToken cancellationToken);
}
