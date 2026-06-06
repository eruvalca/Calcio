using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Teams;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Teams;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Teams;

/// <summary>
/// Registers API endpoints for Teams Endpoints.
/// </summary>
public static class TeamsEndpoints
{
    /// <summary>
    /// Executes the Map Teams Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.Teams.Group)
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetTeams);

        group.MapPost("", CreateTeam);

        return endpoints;
    }

    /// <summary>
    /// Gets teams for a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club whose teams are requested.</param>
    /// <param name="service">The teams service used to retrieve team data.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with teams, or a problem response when retrieval fails.</returns>
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

    /// <summary>
    /// Creates a team for a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club where the team will be created.</param>
    /// <param name="dto">The team creation payload.</param>
    /// <param name="service">The teams service used to create the team.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A created response when the team is created, or a problem response when creation fails.</returns>
    private static async Task<Results<Created, ProblemHttpResult>> CreateTeam(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        CreateTeamDto dto,
        ITeamsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateTeamAsync(clubId, dto, cancellationToken);

        return result.ToHttpResult(TypedResults.Created());
    }
}
