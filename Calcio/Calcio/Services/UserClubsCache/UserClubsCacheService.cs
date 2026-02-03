using Calcio.Data.Contexts;
using Calcio.Shared.Caching;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Extensions.Clubs;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Calcio.Services.UserClubsCache;

/// <summary>
/// Service for caching user club data using HybridCache.
/// Uses user-scoped cache keys and derives the HashSet from the cached list.
/// </summary>
public partial class UserClubsCacheService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    HybridCache cache,
    ILogger<UserClubsCacheService> logger) : IUserClubsCacheService
{
    public async Task<CachedUserClubs> GetUserClubsAsync(long userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheDefaults.Clubs.GetUserClubsKey(userId);

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(ct);

                // Use IgnoreQueryFilters because:
                // 1. We explicitly filter by user membership via the join
                // 2. The DbContext created via factory inside this cache delegate may not have
                //    the correct CurrentUserIdForFilters set (HttpContext may not be available
                //    in the async context when HybridCache executes the factory delegate)
                var clubs = await dbContext.Clubs
                    .IgnoreQueryFilters()
                    .Where(c => c.CalcioUsers.Any(u => u.Id == userId))
                    .OrderBy(c => c.Name)
                    .Select(c => c.ToClubDto())
                    .ToListAsync(ct);

                LogUserClubsLoaded(logger, userId, clubs.Count);

                return CachedUserClubs.FromClubs(clubs);
            },
            options: CacheDefaults.Clubs.EntryOptions,
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<bool> IsUserMemberOfClubAsync(long userId, long clubId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.ClubIds.Contains(clubId);
    }

    public async Task<IReadOnlyList<BaseClubDto>> GetClubsListAsync(long userId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.Clubs;
    }

    public async Task<IReadOnlySet<long>> GetClubIdsAsync(long userId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.ClubIds;
    }

    public async Task InvalidateUserClubsCacheAsync(long userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheDefaults.Clubs.GetUserClubsKey(userId);
        await cache.RemoveAsync(cacheKey, cancellationToken);
        LogUserClubsCacheInvalidated(logger, userId);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded {ClubCount} clubs for user {UserId} from database")]
    private static partial void LogUserClubsLoaded(ILogger logger, long userId, int clubCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invalidated clubs cache for user {UserId}")]
    private static partial void LogUserClubsCacheInvalidated(ILogger logger, long userId);
}
