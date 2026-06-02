using Calcio.Shared.DTOs.Teams;
using Calcio.Entities;

namespace Calcio.Extensions.Teams;

public static class TeamEntityExtensions
{
    extension(TeamEntity team)
    {
        public TeamDto ToTeamDto()
            => new(team.TeamId, team.Name, team.GraduationYear);
    }
}
