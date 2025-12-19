using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Clubs;

public static class ClubMembershipEndpoints
{
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
