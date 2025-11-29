using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;

namespace Calcio.Shared.Services.Seasons;

public interface ISeasonsService
{
    Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken);
}
