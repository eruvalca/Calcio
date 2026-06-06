using Microsoft.Extensions.Caching.Hybrid;

namespace Calcio.Shared.Caching;

/// <summary>
/// Centralized cache configuration defaults organized by domain.
/// Provides consistent cache key prefixes and entry options across the application.
/// </summary>
public static class CacheDefaults
{
    /// <summary>
    /// Cache settings for club-related data.
    /// </summary>
    public static class Clubs
    {
        /// <summary>
        /// Prefix for user clubs cache keys.
        /// </summary>
        public const string UserClubsKeyPrefix = "user-clubs";

        /// <summary>
        /// Default cache duration for user clubs data.
        /// </summary>
        public static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(1);

        /// <summary>
        /// Cache entry options for user clubs data.
        /// </summary>
        public static readonly HybridCacheEntryOptions EntryOptions = new()
        {
            Expiration = DefaultExpiration,
            LocalCacheExpiration = DefaultExpiration
        };

        /// <summary>
        /// Builds a user-scoped cache key for clubs.
        /// </summary>
        public static string GetUserClubsKey(long userId) => $"{UserClubsKeyPrefix}-{userId}";
    }

    /// <summary>
    /// Cache settings for player-related data.
    /// </summary>
    public static class Players
    {
        /// <summary>
        /// Prefix for player photo paths cache keys.
        /// </summary>
        public const string PhotoPathsKeyPrefix = "player-photo-paths";

        /// <summary>
        /// Default cache duration for player photo paths.
        /// </summary>
        public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(55);

        /// <summary>
        /// Cache entry options for player photo paths.
        /// </summary>
        public static readonly HybridCacheEntryOptions EntryOptions = new()
        {
            Expiration = DefaultExpiration,
            LocalCacheExpiration = DefaultExpiration
        };

        /// <summary>
        /// Builds a cache key for player photo paths.
        /// </summary>
        public static string GetPhotoPathsKey(long playerId) => $"{PhotoPathsKeyPrefix}-{playerId}";
    }

    /// <summary>
    /// Cache settings for user-related data.
    /// </summary>
    public static class CalcioUsers
    {
        /// <summary>
        /// Prefix for user photo paths cache keys.
        /// </summary>
        public const string PhotoPathsKeyPrefix = "user-photo-paths";

        /// <summary>
        /// Default cache duration for user photo paths.
        /// </summary>
        public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(55);

        /// <summary>
        /// Cache entry options for user photo paths.
        /// </summary>
        public static readonly HybridCacheEntryOptions EntryOptions = new()
        {
            Expiration = DefaultExpiration,
            LocalCacheExpiration = DefaultExpiration
        };

        /// <summary>
        /// Builds a cache key for user photo paths.
        /// </summary>
        public static string GetPhotoPathsKey(long userId) => $"{PhotoPathsKeyPrefix}-{userId}";
    }
}
