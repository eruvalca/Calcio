using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Players;

public static class PlayersEndpoints
{
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB

    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/clubs/{clubId:long}/players")
            .RequireAuthorization(policy => policy.RequireRole("ClubAdmin"))
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetClubPlayers);

        group.MapPost("", CreatePlayer)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("{playerId:long}/photo", UploadPlayerPhoto)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("{playerId:long}/photo", GetPlayerPhoto)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Ok<List<ClubPlayerDto>>, ProblemHttpResult>> GetClubPlayers(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetClubPlayersAsync(clubId, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Created<PlayerCreatedDto>, ProblemHttpResult>> CreatePlayer(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        CreatePlayerDto dto,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreatePlayerAsync(clubId, dto, cancellationToken);

        return result.ToHttpResult(value => TypedResults.Created($"/api/clubs/{clubId}/players/{value.PlayerId}", value));
    }

    private static async Task<Results<Ok<PlayerPhotoDto>, ProblemHttpResult>> UploadPlayerPhoto(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        [Required]
        [Range(1, long.MaxValue)]
        long playerId,
        IFormFile file,
        IPlayersService service,
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
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, stream, file.ContentType, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Ok<PlayerPhotoDto>, NoContent, ProblemHttpResult>> GetPlayerPhoto(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        [Required]
        [Range(1, long.MaxValue)]
        long playerId,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, cancellationToken);

        return result.Match(
            photoResult => photoResult.Match<Results<Ok<PlayerPhotoDto>, NoContent, ProblemHttpResult>>(
                photo => TypedResults.Ok(photo),
                noPhoto => TypedResults.NoContent()),
            problem => TypedResults.Problem(statusCode: problem.StatusCode, detail: problem.Detail));
    }
}
