using Calcio.Shared.DTOs.Seasons;
using Calcio.Entities;

namespace Calcio.Extensions.Seasons;

public static class SeasonEntityExtensions
{
    extension(SeasonEntity season)
    {
        public SeasonDto ToSeasonDto()
            => new(season.SeasonId, season.Name, season.StartDate, season.EndDate, season.IsComplete);
    }
}
