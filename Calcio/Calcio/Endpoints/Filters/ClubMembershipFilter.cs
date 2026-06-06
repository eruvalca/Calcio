using System.Security.Claims;

using Calcio.Shared.Services.UserClubsCache;

namespace Calcio.Endpoints.Filters;

/// <summary>
/// Endpoint filter that validates the current user has membership access to the club
/// specified in the route's clubId parameter. Returns 403 Forbidden if not a member.
/// </summary>
/// <remarks>
/// This filter uses the cached user clubs data for efficient O(1) membership checks.
/// </remarks>
public sealed partial class ClubMembershipFilter(
    IUserClubsCacheService userClubsCacheService,
    ILogger<ClubMembershipFilter> logger) : IEndpointFilter
{
    /// <summary>
    /// Executes the Invoke Async operation.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="next">The next.</param>
    /// <returns>The operation result.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("clubId", out var clubIdValue)
            || !long.TryParse(clubIdValue?.ToString(), out var clubId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "A valid clubId route parameter is required.");
        }

        if (!long.TryParse(context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var isClubMember = await userClubsCacheService.IsUserMemberOfClubAsync(userId, clubId, context.HttpContext.RequestAborted);

        if (!isClubMember)
        {
            LogForbiddenClubAccess(logger, clubId);
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    /// <summary>
    /// Executes the Log Forbidden Club Access operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Forbidden access attempt to club {ClubId} by authenticated user")]
    /// <summary>
    /// Executes the log forbidden club access operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    private static partial void LogForbiddenClubAccess(ILogger logger, long clubId);
}
