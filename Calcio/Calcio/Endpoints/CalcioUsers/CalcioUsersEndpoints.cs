using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.CalcioUsers;

public static class CalcioUsersEndpoints
{
    public static IEndpointRouteBuilder MapCalcioUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/members")
            .RequireAuthorization(policy => policy.RequireRole("ClubAdmin"))
            .AddEndpointFilter<UnhandledExceptionFilter>();

        group.MapGet("", GetClubMembers);
        group.MapDelete("{userId:long}", RemoveClubMember);

        return endpoints;
    }

    private static async Task<Results<Ok<List<ClubMemberDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetClubMembers(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        ICalcioUsersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubMembersAsync(clubId, cancellationToken);

        return result.Match<Results<Ok<List<ClubMemberDto>>, UnauthorizedHttpResult, ProblemHttpResult>>(
            members => TypedResults.Ok(members),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> RemoveClubMember(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        [Required]
        [Range(1, long.MaxValue)]
        long userId,
        ICalcioUsersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveClubMemberAsync(clubId, userId, cancellationToken);

        return result.Match<Results<NoContent, NotFound, UnauthorizedHttpResult, ProblemHttpResult>>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(),
            unauthorized => TypedResults.Unauthorized(),
            error => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError));
    }
}
