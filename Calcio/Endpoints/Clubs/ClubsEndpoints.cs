using Calcio.Endpoints.Extensions;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Clubs;

/// <summary>
/// Registers API endpoints for Clubs Endpoints.
/// </summary>
public static class ClubsEndpoints
{
    /// <summary>
    /// Executes the Map Clubs Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Gets clubs for the current request scope.
    /// </summary>
    /// <param name="scope">The optional scope selector for user clubs or browsing clubs.</param>
    /// <param name="service">The clubs service used to retrieve clubs.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with clubs, or a problem response when retrieval fails.</returns>
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

    /// <summary>
    /// Creates a new club for the current user.
    /// </summary>
    /// <param name="dto">The club creation payload.</param>
    /// <param name="service">The clubs service used to create the club.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A created response with the created club, or a problem response when creation fails.</returns>
    private static async Task<Results<Created<ClubCreatedDto>, ProblemHttpResult>> CreateClub(
        CreateClubDto dto,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateClubAsync(dto, cancellationToken);

        return result.ToHttpResult(created => TypedResults.Created($"{Routes.Clubs.Base}/{created.ClubId}", created));
    }

    /// <summary>
    /// Gets a club by identifier.
    /// </summary>
    /// <param name="clubId">The identifier of the club to retrieve.</param>
    /// <param name="service">The clubs service used to retrieve the club.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with the requested club, or a problem response when retrieval fails.</returns>
    private static async Task<Results<Ok<BaseClubDto>, ProblemHttpResult>> GetClubById(
        long clubId,
        IClubsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubByIdAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }
}
