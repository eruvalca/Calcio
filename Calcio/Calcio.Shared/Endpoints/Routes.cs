namespace Calcio.Shared.Endpoints;

/// <summary>
/// Centralized API route constants shared between server and client.
/// </summary>
public static class Routes
{
    private const string Api = "api";

    /// <summary>
    /// Route parameter placeholders for endpoint registration.
    /// </summary>
    public static class Parameters
    {
        public const string ClubIdLong = "{clubId:long}";
        public const string PlayerIdLong = "{playerId:long}";
        public const string UserIdLong = "{userId:long}";
        public const string RequestIdLong = "{requestId:long}";
    }

    /// <summary>
    /// Club-related routes.
    /// </summary>
    public static class Clubs
    {
        /// <summary>Base route for club operations: GET (list), POST (create).</summary>
        public const string Base = Api + "/clubs";

        /// <summary>Query parameter name for scope filtering.</summary>
        public const string ScopeQueryName = "scope";

        /// <summary>Value to get all clubs for browsing.</summary>
        public const string ScopeAll = "all";

        /// <summary>Builds the URL for browsing all clubs.</summary>
        public static string ForBrowsing() => $"{Base}?{ScopeQueryName}={ScopeAll}";

        /// <summary>Builds the URL for a specific club.</summary>
        public static string ForClub(long clubId) => $"{Base}/{clubId}";
    }

    /// <summary>
    /// Club member management routes (admin only).
    /// </summary>
    public static class ClubMembers
    {
        /// <summary>Route group: GET (list members), DELETE {userId} (remove member).</summary>
        public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/members";

        /// <summary>Builds the URL for listing members of a club.</summary>
        public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/members";

        /// <summary>Builds the URL for a specific club member.</summary>
        public static string ForMember(long clubId, long userId) => $"{Clubs.Base}/{clubId}/members/{userId}";
    }

    /// <summary>
    /// Current user's club membership routes.
    /// </summary>
    public static class ClubMembership
    {
        /// <summary>Route group: DELETE (leave club).</summary>
        public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/membership";

        /// <summary>Builds the URL for the current user's membership in a club.</summary>
        public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/membership";
    }

    /// <summary>
    /// Club join request routes.
    /// </summary>
    public static class ClubJoinRequests
    {
        /// <summary>Base route for user-facing join request operations.</summary>
        public const string Group = Api + "/club-join-requests";

        /// <summary>GET current user's pending request.</summary>
        public const string GetCurrent = Group + "/current";

        /// <summary>DELETE current user's pending request.</summary>
        public const string CancelCurrent = Group + "/current";

        /// <summary>Builds the URL for creating a join request for a specific club.</summary>
        public static string ForClub(long clubId) => $"{Group}/{clubId}";

        /// <summary>
        /// Admin routes for managing join requests within a club.
        /// </summary>
        public static class Admin
        {
            /// <summary>Route group: GET (list pending), PATCH {requestId} (update status).</summary>
            public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/join-requests";

            /// <summary>Builds the URL for listing pending join requests for a club.</summary>
            public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/join-requests";

            /// <summary>Builds the URL for a specific join request.</summary>
            public static string ForRequest(long clubId, long requestId) => $"{Clubs.Base}/{clubId}/join-requests/{requestId}";
        }
    }

    /// <summary>
    /// Player management routes.
    /// </summary>
    public static class Players
    {
        /// <summary>Route group: GET (list), POST (create), plus sub-routes for photos.</summary>
        public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/players";

        /// <summary>Builds the URL for listing/creating players in a club.</summary>
        public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/players";

        /// <summary>Builds the URL for a specific player.</summary>
        public static string ForPlayer(long clubId, long playerId) => $"{Clubs.Base}/{clubId}/players/{playerId}";

        /// <summary>Builds the URL for a player's photo.</summary>
        public static string ForPlayerPhoto(long clubId, long playerId) => $"{ForPlayer(clubId, playerId)}/photo";
    }

    /// <summary>
    /// Season management routes.
    /// </summary>
    public static class Seasons
    {
        /// <summary>Route group: GET (list), POST (create).</summary>
        public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/seasons";

        /// <summary>Builds the URL for listing/creating seasons in a club.</summary>
        public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/seasons";
    }

    /// <summary>
    /// Team management routes.
    /// </summary>
    public static class Teams
    {
        /// <summary>Route group: GET (list), POST (create).</summary>
        public const string Group = Clubs.Base + "/" + Parameters.ClubIdLong + "/teams";

        /// <summary>Builds the URL for listing/creating teams in a club.</summary>
        public static string ForClub(long clubId) => $"{Clubs.Base}/{clubId}/teams";
    }
}
