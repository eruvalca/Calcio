using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Teams;

public interface ITeamsService
{
    Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken);

    Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken);
}
