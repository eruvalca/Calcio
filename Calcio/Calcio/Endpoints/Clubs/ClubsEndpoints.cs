using Calcio.Endpoints.Extensions;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Clubs;

public static class ClubsEndpoints
{
    public static IEndpointRouteBuilder MapClubsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.Clubs.Base)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, GetClubs);
        group.MapGet("{clubId:long}", GetClubById)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, CreateClub)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<Results<Ok<List<BaseClubDto>>, ProblemHttpResult>> GetClubs(
        string? scope,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = scope.EqualsIgnoreCase(Routes.Clubs.ScopeAll)
            ? await service.GetAllClubsForBrowsingAsync(cancellationToken)
            : await service.GetUserClubsAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Created<ClubCreatedDto>, ProblemHttpResult>> CreateClub(
        CreateClubDto dto,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateClubAsync(dto, cancellationToken);

        return result.ToHttpResult(created => TypedResults.Created($"{Routes.Clubs.Base}/{created.ClubId}", created));
    }

    private static async Task<Results<Ok<BaseClubDto>, ProblemHttpResult>> GetClubById(
        long clubId,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubByIdAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }
}
