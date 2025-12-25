using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.ClubJoinRequests;

public static class ClubJoinRequestsEndpoints
{
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

    private static async Task<Results<Ok<ClubJoinRequestDto>, ProblemHttpResult>> GetCurrentRequest(
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetRequestForCurrentUserAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

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

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelJoinRequest(
        IClubJoinRequestsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        return result.ToHttpResult(TypedResults.NoContent());
    }

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
