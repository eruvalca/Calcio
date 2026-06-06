using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Teams;
using Calcio.Entities;
using Calcio.Extensions.Teams;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Teams;

using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Teams;

/// <summary>
/// Provides Teams Service operations.
/// </summary>
/// <param name="readOnlyDbContextFactory">The read Only Db Context Factory.</param>
/// <param name="readWriteDbContextFactory">The read Write Db Context Factory.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
public partial class TeamsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<TeamsService> logger) : AuthenticatedServiceBase(httpContextAccessor), ITeamsService
{
    /// <summary>
    /// Executes the Get Teams Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Create Team Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Log Teams Retrieved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="count">The count.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} teams for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log teams retrieved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="count">The count.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogTeamsRetrieved(ILogger logger, long clubId, int count, long userId);

    /// <summary>
    /// Executes the Log Team Created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="teamId">The team Id.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created team {TeamId} for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log team created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="teamId">The team id.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogTeamCreated(ILogger logger, long teamId, long clubId, long userId);
}
