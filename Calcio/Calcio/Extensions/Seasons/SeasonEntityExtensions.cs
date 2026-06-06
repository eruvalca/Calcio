using Calcio.Shared.DTOs.Seasons;
using Calcio.Entities;

namespace Calcio.Extensions.Seasons;

/// <summary>
/// Provides extension members for Season Entity Extensions.
/// </summary>
public static class SeasonEntityExtensions
{
    extension(SeasonEntity season)
    {
        /// <summary>
        /// Executes the To Season Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public SeasonDto ToSeasonDto()
            => new(season.SeasonId, season.Name, season.StartDate, season.EndDate, season.IsComplete);
    }
}
