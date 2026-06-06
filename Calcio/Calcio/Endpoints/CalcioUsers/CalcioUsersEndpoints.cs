using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Security;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.CalcioUsers;

/// <summary>
/// Registers API endpoints for Calcio Users Endpoints.
/// </summary>
public static class CalcioUsersEndpoints
{
    /// <summary>
    /// Stores the Max Photo Size.
    /// </summary>
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Executes the Map Calcio Users Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Gets members for the specified club.
    /// </summary>
    /// <param name="clubId">The identifier of the club whose members are requested.</param>
    /// <param name="service">The service used to retrieve club member data.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with club members, or a problem response when retrieval fails.</returns>
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

    /// <summary>
    /// Removes a club member from the specified club.
    /// </summary>
    /// <param name="clubId">The identifier of the club to remove the member from.</param>
    /// <param name="userId">The identifier of the user to remove.</param>
    /// <param name="service">The service used to perform the membership removal.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A no-content response when removal succeeds, or a problem response when removal fails.</returns>
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

    /// <summary>
    /// Uploads the authenticated user's account profile photo.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="service">The service used to persist the account photo.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with the uploaded photo metadata, or a problem response when validation or upload fails.</returns>
    private static async Task<Results<Ok<CalcioUserPhotoDto>, ProblemHttpResult>> UploadAccountPhoto(
        IFormFile file,
        ICalcioUsersService service,
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

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Gets the authenticated user's account profile photo.
    /// </summary>
    /// <param name="service">The service used to retrieve the account photo.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// An OK response with photo metadata when a photo exists, no-content when no photo is stored, or a problem response when retrieval fails.
    /// </returns>
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
