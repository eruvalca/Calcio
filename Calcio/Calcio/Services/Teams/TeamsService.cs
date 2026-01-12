using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Entities;
using Calcio.Shared.Extensions.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Teams;

public partial class TeamsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
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

    public async Task<ServiceResult<Success>> CreateTeamAsync(long clubId, CreateTeamDto dto, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = new TeamEntity
        {
            Name = dto.Name,
            GraduationYear = dto.GraduationYear,
            ClubId = clubId,
            CreatedById = CurrentUserId
        };

        await dbContext.Teams.AddAsync(team, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogTeamCreated(logger, team.TeamId, clubId, CurrentUserId);
        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} teams for club {ClubId} by user {UserId}")]
    private static partial void LogTeamsRetrieved(ILogger logger, long clubId, int count, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created team {TeamId} for club {ClubId} by user {UserId}")]
    private static partial void LogTeamCreated(ILogger logger, long teamId, long clubId, long userId);
}
