using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Models.Entities;

namespace Calcio.Shared.Extensions.Seasons;

public static class SeasonEntityExtensions
{
    extension(SeasonEntity season)
    {
        public SeasonDto ToSeasonDto()
            => new(season.SeasonId, season.Name, season.StartDate, season.EndDate, season.IsComplete);
    }
}
