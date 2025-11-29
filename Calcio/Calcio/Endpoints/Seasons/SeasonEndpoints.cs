using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Services.Seasons;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Seasons;

public static class SeasonEndpoints
{
    public static IEndpointRouteBuilder MapSeasonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/seasons")
            .RequireAuthorization()
            .AddEndpointFilter<UnhandledExceptionFilter>();

        group.MapGet("", GetSeasons);

        return endpoints;
    }

    private static async Task<Results<Ok<List<SeasonDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetSeasons(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        ISeasonService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSeasonsAsync(clubId, cancellationToken);

        return result.Match<Results<Ok<List<SeasonDto>>, UnauthorizedHttpResult, ProblemHttpResult>>(
            seasons => TypedResults.Ok(seasons),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }
}
