using Calcio.Data.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Endpoints.Filters;

/// <summary>
/// Endpoint filter that validates the current user has membership access to the club
/// specified in the route's clubId parameter. Returns 403 Forbidden if not a member.
/// </summary>
/// <remarks>
/// This filter relies on global query filters configured in <see cref="BaseDbContext"/>
/// which automatically restrict club queries to those accessible by the current user.
/// </remarks>
public sealed partial class ClubMembershipFilter(
    IDbContextFactory<ReadOnlyDbContext> dbContextFactory,
    ILogger<ClubMembershipFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("clubId", out var clubIdValue)
            || !long.TryParse(clubIdValue?.ToString(), out var clubId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "A valid clubId route parameter is required.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(context.HttpContext.RequestAborted);

        // Global query filters on Clubs automatically restrict to clubs the current user belongs to.
        // If the club isn't found, the user either doesn't have access or it doesn't exist.
        var isClubMember = await dbContext.Clubs.AnyAsync(c => c.ClubId == clubId, context.HttpContext.RequestAborted);

        if (!isClubMember)
        {
            LogForbiddenClubAccess(logger, clubId);
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Forbidden access attempt to club {ClubId} by authenticated user")]
    private static partial void LogForbiddenClubAccess(ILogger logger, long clubId);
}
