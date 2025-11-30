using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Services.Seasons;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Seasons;

public static class SeasonsEndpoints
{
    public static IEndpointRouteBuilder MapSeasonsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/seasons")
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetSeasons);

        var clubAdminGroup = endpoints.MapGroup("api/clubs/{clubId:long}/seasons")
            .RequireAuthorization(policy => policy.RequireRole("ClubAdmin"))
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        clubAdminGroup.MapPost("", CreateSeason);

        return endpoints;
    }

    private static async Task<Results<Ok<List<SeasonDto>>, ProblemHttpResult>> GetSeasons(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        ISeasonsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSeasonsAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Created, ProblemHttpResult>> CreateSeason(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        CreateSeasonDto dto,
        ISeasonsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateSeasonAsync(clubId, dto, cancellationToken);

        return result.ToHttpResult(TypedResults.Created());
    }
}
