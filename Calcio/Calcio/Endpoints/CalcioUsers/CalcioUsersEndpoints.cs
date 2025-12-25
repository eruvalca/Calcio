using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Security;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.CalcioUsers;

public static class CalcioUsersEndpoints
{
    public static IEndpointRouteBuilder MapCalcioUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.ClubMembers.Group)
            .RequireAuthorization(policy => policy.RequireRole(Roles.ClubAdmin))
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetClubMembers);
        group.MapDelete("{userId:long}", RemoveClubMember)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Ok<List<ClubMemberDto>>, ProblemHttpResult>> GetClubMembers(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        ICalcioUsersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubMembersAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveClubMember(
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

        return result.ToHttpResult(TypedResults.NoContent());
    }
}
