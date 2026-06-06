using System.ComponentModel;

using Calcio.Shared.DTOs.Clubs;

namespace Calcio.Shared.Caching;

/// <summary>
/// Cached representation of a user's clubs.
/// Marked as sealed and immutable for HybridCache instance reuse.
/// </summary>
[ImmutableObject(true)]
public sealed record CachedUserClubs
{
    /// <summary>
    /// The list of clubs the user belongs to.
    /// </summary>
    public required IReadOnlyList<BaseClubDto> Clubs { get; init; }

    /// <summary>
    /// Set of club IDs for O(1) membership checks.
    /// </summary>
    public required IReadOnlySet<long> ClubIds { get; init; }

    /// <summary>
    /// Creates a new instance from a list of clubs.
    /// </summary>
    public static CachedUserClubs FromClubs(IEnumerable<BaseClubDto> clubs)
    {
        var clubList = clubs.ToList();
        return new CachedUserClubs
        {
            Clubs = clubList,
            ClubIds = clubList.Select(c => c.Id).ToHashSet()
        };
    }
}
