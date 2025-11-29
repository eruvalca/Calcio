using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.Seasons;

public interface ISeasonsService
{
    Task<OneOf<List<SeasonDto>, Unauthorized, Error>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken);
}
