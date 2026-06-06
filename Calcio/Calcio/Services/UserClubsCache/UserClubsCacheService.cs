using Calcio.Data.Contexts;
using Calcio.Shared.Caching;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Extensions.Clubs;
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
    /// <summary>
    /// Executes the Get User Clubs Async operation.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Is User Member Of Club Async operation.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> IsUserMemberOfClubAsync(long userId, long clubId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.ClubIds.Contains(clubId);
    }

    /// <summary>
    /// Executes the Get Clubs List Async operation.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<BaseClubDto>> GetClubsListAsync(long userId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.Clubs;
    }

    /// <summary>
    /// Executes the Get Club Ids Async operation.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlySet<long>> GetClubIdsAsync(long userId, CancellationToken cancellationToken)
    {
        var cachedClubs = await GetUserClubsAsync(userId, cancellationToken);
        return cachedClubs.ClubIds;
    }

    /// <summary>
    /// Executes the Invalidate User Clubs Cache Async operation.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task InvalidateUserClubsCacheAsync(long userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheDefaults.Clubs.GetUserClubsKey(userId);
        await cache.RemoveAsync(cacheKey, cancellationToken);
        LogUserClubsCacheInvalidated(logger, userId);
    }

    /// <summary>
    /// Executes the Log User Clubs Loaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="clubCount">The club Count.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded {ClubCount} clubs for user {UserId} from database")]
    /// <summary>
    /// Executes the log user clubs loaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="clubCount">The club count.</param>
    private static partial void LogUserClubsLoaded(ILogger logger, long userId, int clubCount);

    /// <summary>
    /// Executes the Log User Clubs Cache Invalidated operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Invalidated clubs cache for user {UserId}")]
    /// <summary>
    /// Executes the log user clubs cache invalidated operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserClubsCacheInvalidated(ILogger logger, long userId);
}
