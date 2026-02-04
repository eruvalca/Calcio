using System.ComponentModel.DataAnnotations;

using Calcio.Endpoints.Extensions;
using Calcio.Endpoints.Filters;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Services.Players;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Calcio.Endpoints.Players;

public static class PlayersEndpoints
{
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB
    private const long MaxImportFileSize = 10 * 1024 * 1024; // 10 MB

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

        return result.ToHttpResult(value => TypedResults.Created($"{Routes.Clubs.Base}/{clubId}/players/{value.PlayerId}", value));
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

        var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "File type not allowed. Please upload a CSV or Excel file (.csv, .xlsx, .xls).");
        }

        await using var stream = file.OpenReadStream();
        var result = await service.ValidateBulkImportAsync(clubId, stream, file.FileName, cancellationToken);

        return result.ToHttpResult(TypedResults.Ok);
    }

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

    private static FileContentHttpResult GetImportTemplate(
        [Required]
        [Range(1, long.MaxValue)]
        long clubId,
        [FromQuery] string format,
        IPlayerImportTemplateService templateService) => format?.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => TypedResults.File(
                templateService.GenerateExcelTemplate(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "player_import_template.xlsx"),
            _ => TypedResults.File(
                templateService.GenerateCsvTemplate(),
                "text/csv",
                "player_import_template.csv")
        };
}
