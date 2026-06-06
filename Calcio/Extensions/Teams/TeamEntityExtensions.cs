using Calcio.Shared.DTOs.Teams;
using Calcio.Entities;

namespace Calcio.Extensions.Teams;

/// <summary>
/// Provides extension members for Team Entity Extensions.
/// </summary>
public static class TeamEntityExtensions
{
    extension(TeamEntity team)
    {
        /// <summary>
        /// Executes the To Team Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public TeamDto ToTeamDto()
            => new(team.TeamId, team.Name, team.GraduationYear);
    }
}
