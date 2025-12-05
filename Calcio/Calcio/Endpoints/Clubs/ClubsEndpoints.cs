using Calcio.Endpoints.Extensions;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Clubs;

public static class ClubsEndpoints
{
    public static IEndpointRouteBuilder MapClubsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetUserClubs);
        group.MapGet("all", GetAllClubsForBrowsing);
        group.MapPost("", CreateClub)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<Results<Ok<List<BaseClubDto>>, ProblemHttpResult>> GetUserClubs(
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetUserClubsAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Ok<List<BaseClubDto>>, ProblemHttpResult>> GetAllClubsForBrowsing(
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllClubsForBrowsingAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Created<ClubCreatedDto>, ProblemHttpResult>> CreateClub(
        CreateClubDto dto,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateClubAsync(dto, cancellationToken);

        return result.ToHttpResult(created => TypedResults.Created($"api/clubs/{created.ClubId}", created));
    }
}
