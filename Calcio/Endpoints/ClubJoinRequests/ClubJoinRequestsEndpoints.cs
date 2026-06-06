using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.ClubJoinRequests;

/// <summary>
/// Registers API endpoints for Club Join Requests Endpoints.
/// </summary>
public static class ClubJoinRequestsEndpoints
{
    /// <summary>
    /// Executes the Map Club Join Requests Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapClubJoinRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.ClubJoinRequests.Group)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("current", GetCurrentRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("{clubId:long}", CreateJoinRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("current", CancelJoinRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var clubAdminGroup = endpoints.MapGroup(Routes.ClubJoinRequests.Admin.Group)
            .RequireAuthorization(policy => policy.RequireRole(Roles.ClubAdmin))
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        clubAdminGroup.MapGet("", GetPendingRequestsForClub);

        // Canonical RESTful route: update the join request resource (e.g. status)
        clubAdminGroup.MapPatch(Routes.Parameters.RequestIdLong, UpdateJoinRequestStatus)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    /// <summary>
    /// Gets the current user's active club join request.
    /// </summary>
    /// <param name="service">The service used to load the current user's join request.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with the current request, or a problem response when retrieval fails.</returns>
    private static async Task<Results<Ok<ClubJoinRequestDto>, ProblemHttpResult>> GetCurrentRequest(
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetRequestForCurrentUserAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Creates a join request for the specified club.
    /// </summary>
    /// <param name="clubId">The identifier of the club to join.</param>
    /// <param name="service">The service used to create the join request.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A created response when the request is created, or a problem response when creation fails.</returns>
    private static async Task<Results<Created, ProblemHttpResult>> CreateJoinRequest(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateJoinRequestAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Created());
    }

    /// <summary>
    /// Cancels the current user's pending join request.
    /// </summary>
    /// <param name="service">The service used to cancel the join request.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A no-content response when cancellation succeeds, or a problem response when cancellation fails.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> CancelJoinRequest(
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }

    /// <summary>
    /// Gets pending join requests for the specified club.
    /// </summary>
    /// <param name="clubId">The identifier of the club whose pending requests are requested.</param>
    /// <param name="service">The service used to retrieve pending join requests.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with pending requests, or a problem response when retrieval fails.</returns>
    private static async Task<Results<Ok<List<ClubJoinRequestWithUserDto>>, ProblemHttpResult>> GetPendingRequestsForClub(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPendingRequestsForClubAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Updates the status of a club join request.
    /// </summary>
    /// <param name="clubId">The identifier of the club that owns the request.</param>
    /// <param name="requestId">The identifier of the join request to update.</param>
    /// <param name="dto">The requested status update payload.</param>
    /// <param name="service">The service used to apply the status update.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A no-content response when the update succeeds, or a problem response when the update fails.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateJoinRequestStatus(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        [Required]
        [Range(1, long.MaxValue)]
        long requestId,
        UpdateClubJoinRequestStatusDto dto,
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, dto.Status, cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }
}
