using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Clubs;

/// <summary>
/// Registers API endpoints for Club Membership Endpoints.
/// </summary>
public static class ClubMembershipEndpoints
{
    /// <summary>
    /// Executes the Map Club Membership Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapClubMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.ClubMembership.Group)
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete(string.Empty, LeaveClub)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    /// <summary>
    /// Removes the current user from the specified club.
    /// </summary>
    /// <param name="clubId">The identifier of the club to leave.</param>
    /// <param name="service">The service used to process the leave-club operation.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A no-content response when the user leaves the club, or a problem response when the operation fails.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> LeaveClub(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LeaveClubAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}
