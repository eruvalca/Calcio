using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.CalcioUsers;

public interface ICalcioUsersService
{
    Task<OneOf<List<ClubMemberDto>, Unauthorized, Error>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken);
    Task<OneOf<Success, NotFound, Unauthorized, Error>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken);
}
