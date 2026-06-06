using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calcio.Endpoints.Players;

/// <summary>
/// Registers API endpoints for Players Endpoints.
/// </summary>
public static class PlayersEndpoints
{
    /// <summary>
    /// Stores the Max Photo Size.
    /// </summary>
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB
    /// <summary>
    /// Stores the Max Import File Size.
    /// </summary>
    private const long MaxImportFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Executes the Map Players Endpoints operation.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <returns>The operation result.</returns>
    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Routes.Players.Group)
            .RequireAuthorization()
            .AddEndpointFilter<ClubMembershipFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", GetClubPlayers);

        group.MapPost("", CreatePlayer)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Canonical RESTful route for a single photo resource
        group.MapPut("{playerId:long}/photo", UploadPlayerPhoto)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("{playerId:long}/photo", GetPlayerPhoto)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Bulk import endpoints
        group.MapPost("bulk/validate", ValidateBulkImport)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("bulk/revalidate", RevalidateBulkImport)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("bulk", ExecuteBulkImport)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("bulk/template", GetImportTemplate);

        return endpoints;
    }

    /// <summary>
    /// Gets players for a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club whose players are requested.</param>
    /// <param name="service">The players service used to retrieve player data.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with club players, or a problem response when retrieval fails.</returns>
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

    /// <summary>
    /// Creates a player in a specific club.
    /// </summary>
    /// <param name="clubId">The identifier of the club where the player will be created.</param>
    /// <param name="dto">The player creation payload.</param>
    /// <param name="service">The players service used to create the player.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>A created response with the created player, or a problem response when creation fails.</returns>
    private static async Task<Results<Created<PlayerCreatedDto>, ProblemHttpResult>> CreatePlayer(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        CreatePlayerDto dto,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreatePlayerAsync(clubId, dto, cancellationToken);

        return result.ToHttpResult(value => TypedResults.Created($"{Routes.Clubs.Base}/{clubId}/players/{value.PlayerId}", value));
    }

    /// <summary>
    /// Uploads a player's photo for a club.
    /// </summary>
    /// <param name="clubId">The identifier of the club that owns the player.</param>
    /// <param name="playerId">The identifier of the player whose photo is uploaded.</param>
    /// <param name="file">The image file to upload.</param>
    /// <param name="service">The players service used to store the photo.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with photo metadata, or a problem response when validation or upload fails.</returns>
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

    /// <summary>
    /// Gets a player's photo for a club.
    /// </summary>
    /// <param name="clubId">The identifier of the club that owns the player.</param>
    /// <param name="playerId">The identifier of the player whose photo is requested.</param>
    /// <param name="service">The players service used to retrieve the photo.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// An OK response with photo metadata when present, no-content when no photo exists, or a problem response when retrieval fails.
    /// </returns>
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

    /// <summary>
    /// Validates a bulk player import file.
    /// </summary>
    /// <param name="clubId">The identifier of the club where players are being imported.</param>
    /// <param name="file">The CSV file to validate.</param>
    /// <param name="service">The players service used to validate import rows.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with validation results, or a problem response when validation fails.</returns>
    private static async Task<Results<Ok<BulkValidateResultDto>, ProblemHttpResult>> ValidateBulkImport(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IFormFile file,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File is empty or missing.");
        }

        if (file.Length > MaxImportFileSize)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"File size exceeds maximum of {MaxImportFileSize / 1024 / 1024} MB.");
        }

        var allowedExtensions = new[] { ".csv" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File type not allowed. Please upload a CSV file (.csv). Save Excel or Google Sheets files as CSV before uploading.");
        }

        await using var stream = file.OpenReadStream();
        var result = await service.ValidateBulkImportAsync(clubId, stream, file.FileName, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Revalidates edited bulk import rows before execution.
    /// </summary>
    /// <param name="clubId">The identifier of the club where players are being imported.</param>
    /// <param name="rows">The rows to revalidate.</param>
    /// <param name="service">The players service used to revalidate rows.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with validation results, or a problem response when revalidation fails.</returns>
    private static async Task<Results<Ok<BulkValidateResultDto>, ProblemHttpResult>> RevalidateBulkImport(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        List<PlayerImportRowDto> rows,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "No rows provided for re-validation.");
        }

        var result = await service.RevalidateBulkImportAsync(clubId, rows, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Executes bulk player import for validated rows.
    /// </summary>
    /// <param name="clubId">The identifier of the club where players are being imported.</param>
    /// <param name="request">The bulk import request containing rows to import.</param>
    /// <param name="service">The players service used to execute the import.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>An OK response with import results, or a problem response when import fails.</returns>
    private static async Task<Results<Ok<BulkImportResultDto>, ProblemHttpResult>> ExecuteBulkImport(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        BulkImportPlayersRequest request,
        IPlayersService service,
        CancellationToken cancellationToken)
    {
        if (request?.Rows is null || request.Rows.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "No rows provided for import.");
        }

        var result = await service.BulkCreatePlayersAsync(clubId, request.Rows, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>
    /// Gets the CSV template used for bulk player import.
    /// </summary>
    /// <param name="clubId">The identifier of the club requesting the template.</param>
    /// <param name="templateService">The template service used to generate the CSV template.</param>
    /// <returns>A file response containing the bulk import template.</returns>
    private static FileContentHttpResult GetImportTemplate(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        IPlayerImportTemplateService templateService) => TypedResults.File(
            templateService.GenerateCsvTemplate(),
            "text/csv",
            "player_import_template.csv");
}
