using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Services.Teams;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Teams;

public static class TeamsEndpoints
{
    public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/teams")
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetTeams);

        return endpoints;
    }

    private static async Task<Results<Ok<List<TeamDto>>, ProblemHttpResult>> GetTeams(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        ITeamsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetTeamsAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }
}
