using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Players;

public static class PlayersEndpoints
{
    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/players")
            .RequireAuthorization(policy => policy.RequireRole("ClubAdmin"))
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetClubPlayers);

        return endpoints;
    }

    private static async Task<Results<Ok<List<ClubPlayerDto>>, ProblemHttpResult>> GetClubPlayers(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubPlayersAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }
}
