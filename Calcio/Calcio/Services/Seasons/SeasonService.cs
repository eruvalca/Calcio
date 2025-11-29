using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Extensions.Seasons;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using Microsoft.EntityFrameworkCore;

using OneOf;
using OneOf.Types;

namespace Calcio.Services.Seasons;

public partial class SeasonService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SeasonService> logger) : AuthenticatedServiceBase(httpContextAccessor), ISeasonService
{
    public async Task<OneOf<List<SeasonDto>, Unauthorized, Error>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isClubMember = await dbContext.Clubs
            .AnyAsync(c => c.ClubId == clubId, cancellationToken);

        if (!isClubMember)
        {
            return new Unauthorized();
        }

        var seasons = await dbContext.Seasons
            .Where(s => s.ClubId == clubId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.ToSeasonDto())
            .ToListAsync(cancellationToken);

        LogSeasonsRetrieved(logger, clubId, seasons.Count, CurrentUserId);
        return seasons;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {Count} seasons for club {ClubId} by user {UserId}")]
    private static partial void LogSeasonsRetrieved(ILogger logger, long clubId, int count, long userId);
}
