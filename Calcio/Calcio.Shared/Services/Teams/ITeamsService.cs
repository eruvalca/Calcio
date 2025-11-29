using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.Teams;

public interface ITeamsService
{
    Task<OneOf<List<TeamDto>, Unauthorized, Error>> GetTeamsAsync(long clubId, CancellationToken cancellationToken);
}
