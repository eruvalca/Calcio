using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.ClubJoinRequests;

public interface IClubJoinRequestsService
{
    Task<ServiceResult<ClubJoinRequestDto>> GetRequestForCurrentUserAsync(CancellationToken cancellationToken);
    Task<ServiceResult<Success>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken);
    Task<ServiceResult<Success>> CancelJoinRequestAsync(CancellationToken cancellationToken);
    Task<ServiceResult<List<ClubJoinRequestWithUserDto>>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken);
    Task<ServiceResult<Success>> UpdateJoinRequestStatusAsync(long clubId, long requestId, RequestStatus status, CancellationToken cancellationToken);
}
