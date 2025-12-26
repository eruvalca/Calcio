using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Entities;
using Calcio.Shared.Extensions.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using Microsoft.EntityFrameworkCore;

using OneOf.Types;

namespace Calcio.Services.Seasons;

public partial class SeasonsService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SeasonsService> logger) : AuthenticatedServiceBase(httpContextAccessor), ISeasonsService
{
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

        dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogSeasonCreated(logger, season.SeasonId, clubId, CurrentUserId);
        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} seasons for club {ClubId} by user {UserId}")]
    private static partial void LogSeasonsRetrieved(ILogger logger, long clubId, int count, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created season {SeasonId} for club {ClubId} by user {UserId}")]
    private static partial void LogSeasonCreated(ILogger logger, long seasonId, long clubId, long userId);
}
