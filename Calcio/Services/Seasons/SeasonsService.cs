using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Entities;
using Calcio.Extensions.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Seasons;

/// <summary>
/// Provides Seasons Service operations.
/// </summary>
/// <param name="readOnlyDbContextFactory">The read Only Db Context Factory.</param>
/// <param name="readWriteDbContextFactory">The read Write Db Context Factory.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
public partial class SeasonsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SeasonsService> logger) : AuthenticatedServiceBase(httpContextAccessor), ISeasonsService
{
    /// <summary>
    /// Executes the Get Seasons Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var seasons = await dbContext.Seasons
            .Where(s => s.ClubId == clubId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.ToSeasonDto())
            .ToListAsync(cancellationToken);

        LogSeasonsRetrieved(logger, clubId, seasons.Count, CurrentUserId);
        return seasons;
    }

    /// <summary>
    /// Executes the Create Season Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var season = new SeasonEntity
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ClubId = clubId,
            CreatedById = CurrentUserId
        };

        await dbContext.Seasons.AddAsync(season, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogSeasonCreated(logger, season.SeasonId, clubId, CurrentUserId);
        return new Success();
    }

    /// <summary>
    /// Executes the Log Seasons Retrieved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="count">The count.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} seasons for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log seasons retrieved operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="count">The count.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogSeasonsRetrieved(ILogger logger, long clubId, int count, long userId);

    /// <summary>
    /// Executes the Log Season Created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="seasonId">The season Id.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Created season {SeasonId} for club {ClubId} by user {UserId}")]
    /// <summary>
    /// Executes the log season created operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="seasonId">The season id.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogSeasonCreated(ILogger logger, long seasonId, long clubId, long userId);
}
