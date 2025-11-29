using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.Players;

public interface IPlayersService
{
    Task<OneOf<List<ClubPlayerDto>, Unauthorized, Error>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken);
}
