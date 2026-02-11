using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Hubs;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Realtime;
using Calcio.Shared.Security;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;

namespace Calcio.Endpoints.CalcioUsers;

public static class CalcioUsersEndpoints
{
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB

    public static IEndpointRouteBuilder MapCalcioUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var clubMembersGroup = endpoints.MapGroup(Routes.ClubMembers.Group)
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        clubMembersGroup.MapGet("", GetClubMembers);

        clubMembersGroup.MapDelete("{userId:long}", RemoveClubMember)
            .RequireAuthorization(policy => policy.RequireRole(Roles.ClubAdmin))
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Account photo endpoints - requires only authentication, no club membership
        var accountPhotoGroup = endpoints.MapGroup(Routes.Account.PhotoGroup)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        accountPhotoGroup.MapPut("", UploadAccountPhoto)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        accountPhotoGroup.MapGet("", GetAccountPhoto);

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

    private static async Task<Results<Ok<CalcioUserPhotoDto>, ProblemHttpResult>> UploadAccountPhoto(
        IFormFile file,
        ICalcioUsersService service,
        IHubContext<UserPhotoHub> hubContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Validate file
        if (file is null || file.Length == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File is empty or missing.");
        }

        if (file.Length > MaxPhotoSize)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"File size exceeds maximum of {MaxPhotoSize / 1024 / 1024} MB.");
        }

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File type not allowed. Allowed types: JPEG, PNG, GIF, WebP.");
        }

        await using var stream = file.OpenReadStream();
        var result = await service.UploadAccountPhotoAsync(stream, file.ContentType, cancellationToken);

        if (result.IsSuccess)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                await hubContext.Clients.User(userId)
                    .SendAsync(UserPhotoHubMessages.PhotoChanged, Array.Empty<object?>(), cancellationToken);
            }
        }

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Ok<CalcioUserPhotoDto>, NoContent, ProblemHttpResult>> GetAccountPhoto(
        ICalcioUsersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAccountPhotoAsync(cancellationToken);

        return result.Match(
            photoResult => photoResult.Match<Results<Ok<CalcioUserPhotoDto>, NoContent, ProblemHttpResult>>(
                photo => TypedResults.Ok(photo),
                noPhoto => TypedResults.NoContent()),
            problem => TypedResults.Problem(statusCode: problem.StatusCode, detail: problem.Detail));
    }
}
