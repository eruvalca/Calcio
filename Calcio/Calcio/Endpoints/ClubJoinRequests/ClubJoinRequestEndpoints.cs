using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Http.HttpResults;

using ConflictResult = Microsoft.AspNetCore.Http.HttpResults.Conflict;

namespace Calcio.Endpoints.ClubJoinRequests;

public static class ClubJoinRequestEndpoints
{
    public static IEndpointRouteBuilder MapClubJoinRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/club-join-requests")
            .RequireAuthorization()
            .AddEndpointFilter<UnhandledExceptionFilter>();

        group.MapGet("pending", GetPendingRequest);
        group.MapPost("{clubId:long}", CreateJoinRequest);
        group.MapDelete("pending", CancelJoinRequest);

        var clubAdminGroup = endpoints.MapGroup("api/clubs/{clubId:long}/join-requests")
            .RequireAuthorization(policy => policy.RequireRole("ClubAdmin"))
            .AddEndpointFilter<UnhandledExceptionFilter>();

        clubAdminGroup.MapGet("", GetPendingRequestsForClub);
        clubAdminGroup.MapPost("{requestId:long}/approve", ApproveJoinRequest);
        clubAdminGroup.MapPost("{requestId:long}/reject", RejectJoinRequest);

        return endpoints;
    }

    private static async Task<Results<Ok<ClubJoinRequestDto>, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> GetPendingRequest(
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var request = await service.GetPendingRequestForCurrentUserAsync(cancellationToken);

        return request.Match<Results<Ok<ClubJoinRequestDto>, NotFound, UnauthorizedHttpResult, ProblemHttpResult>>(
            dto => TypedResults.Ok(dto),
            notFound => TypedResults.NotFound(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<Created, NotFound, ConflictResult, UnauthorizedHttpResult, ProblemHttpResult>> CreateJoinRequest(
        long clubId,
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateJoinRequestAsync(clubId, cancellationToken);

        return result.Match<Results<Created, NotFound, ConflictResult, UnauthorizedHttpResult, ProblemHttpResult>>(
            success => TypedResults.Created(),
            notFound => TypedResults.NotFound(),
            conflict => TypedResults.Conflict(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> CancelJoinRequest(
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelJoinRequestAsync(cancellationToken);

        return result.Match<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<Ok<List<ClubJoinRequestWithUserDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetPendingRequestsForClub(
        long clubId,
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPendingRequestsForClubAsync(clubId, cancellationToken);

        return result.Match<Results<Ok<List<ClubJoinRequestWithUserDto>>, UnauthorizedHttpResult, ProblemHttpResult>>(
            requests => TypedResults.Ok(requests),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> ApproveJoinRequest(
        long clubId,
        long requestId,
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveJoinRequestAsync(clubId, requestId, cancellationToken);

        return result.Match<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> RejectJoinRequest(
        long clubId,
        long requestId,
        IClubJoinRequestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RejectJoinRequestAsync(clubId, requestId, cancellationToken);

        return result.Match<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }
}
