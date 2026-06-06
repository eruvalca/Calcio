using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Seasons;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Seasons;

/// <summary>
/// Registers API endpoints for Seasons Endpoints.
/// </summary>
public static class SeasonsEndpoints
{
    /// <summary>
    /// Executes the Map Seasons Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapSeasonsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.Seasons.Group)
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetSeasons);

        group.MapPost("", CreateSeason);

        return endpoints;
    }

    /// <summary>
    /// Gets seasons for a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club whose seasons are requested.</param>
    /// <param name="service">The seasons service used to retrieve season data.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with seasons, or a problem response when retrieval fails.</returns>
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

    /// <summary>
    /// Creates a season for a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club where the season will be created.</param>
    /// <param name="dto">The season creation payload.</param>
    /// <param name="service">The seasons service used to create the season.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A created response when the season is created, or a problem response when creation fails.</returns>
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
