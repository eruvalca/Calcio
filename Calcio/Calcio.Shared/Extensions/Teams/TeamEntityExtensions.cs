using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Entities;

namespace Calcio.Shared.Extensions.Teams;

public static class TeamEntityExtensions
{
    extension(TeamEntity team)
    {
        public TeamDto ToTeamDto()
            => new(team.TeamId, team.Name, team.GraduationYear);
    }
}
