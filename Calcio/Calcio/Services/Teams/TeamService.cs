using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Extensions.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.Teams;

public partial class TeamService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<TeamService> logger) : AuthenticatedServiceBase(httpContextAccessor), ITeamService
{
    public async Task<OneOf<List<TeamDto>, Unauthorized, Error>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var teams = await dbContext.Teams
            .Where(t => t.ClubId == clubId)
            .OrderBy(t => t.Name)
            .Select(t => t.ToTeamDto())
            .ToListAsync(cancellationToken);

        LogTeamsRetrieved(logger, clubId, teams.Count, CurrentUserId);
        return teams;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} teams for club {ClubId} by user {UserId}")]
    private static partial void LogTeamsRetrieved(ILogger logger, long clubId, int count, long userId);
}
