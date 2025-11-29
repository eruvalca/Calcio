using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Extensions.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Services.Teams;

public partial class TeamsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<TeamsService> logger) : AuthenticatedServiceBase(httpContextAccessor), ITeamsService
{
    public async Task<ServiceResult<List<TeamDto>>> GetTeamsAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

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
