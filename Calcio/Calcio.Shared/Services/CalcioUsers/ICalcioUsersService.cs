using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.CalcioUsers;

public interface ICalcioUsersService
{
    Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken);
    Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken);
}
