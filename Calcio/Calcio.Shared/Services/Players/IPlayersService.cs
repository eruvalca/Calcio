using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Results;

namespace Calcio.Shared.Services.Players;

public interface IPlayersService
{
    Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken);
}
