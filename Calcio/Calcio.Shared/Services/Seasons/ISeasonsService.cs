using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Seasons;

public interface ISeasonsService
{
    Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken);

    Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken);
}
