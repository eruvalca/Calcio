using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;

namespace Calcio.Shared.Services.Teams;

public interface ITeamsService
{
    Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken);
}
