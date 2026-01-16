using Calcio.Shared.Caching;
using Calcio.Shared.DTOs.Clubs;

namespace Calcio.Shared.Services.UserClubsCache;

/// <summary>
/// Service for caching user club data with user-scoped keys.
/// Provides efficient access to a user's clubs and membership checks.
/// </summary>
public interface IUserClubsCacheService
{
    /// <summary>
    /// Gets the cached clubs for a user, loading from the database if not cached.
    /// </summary>
    /// <param name="userId">The user ID to get clubs for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached user clubs data.</returns>
    Task<CachedUserClubs> GetUserClubsAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a user is a member of a specific club using cached data.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="clubId">The club ID to check membership for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is a member of the club.</returns>
    Task<bool> IsUserMemberOfClubAsync(long userId, long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of clubs for a user.
    /// </summary>
    /// <param name="userId">The user ID to get clubs for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of clubs the user belongs to.</returns>
    Task<IReadOnlyList<BaseClubDto>> GetClubsListAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the set of club IDs for a user for O(1) membership checks.
    /// </summary>
    /// <param name="userId">The user ID to get club IDs for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The set of club IDs the user belongs to.</returns>
    Task<IReadOnlySet<long>> GetClubIdsAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates the cached clubs for a user.
    /// Should be called when a user joins or leaves a club.
    /// </summary>
    /// <param name="userId">The user ID to invalidate cache for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateUserClubsCacheAsync(long userId, CancellationToken cancellationToken);
}
