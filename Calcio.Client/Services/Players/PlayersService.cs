using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Players;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.Players;

/// <summary>
/// Provides client-side player management operations, including photos and bulk import workflows.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class PlayersService(HttpClient httpClient) : IPlayersService
{
    /// <summary>
    /// Retrieves all players for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club whose players are requested.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of players when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Players.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<List<ClubPlayerDto>>)(await response.Content.ReadFromJsonAsync<List<ClubPlayerDto>>(cancellationToken) ?? [])
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new player for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club where the player is created.</param>
    /// <param name="dto">Payload describing the player to create.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Created player metadata when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<PlayerCreatedDto>> CreatePlayerAsync(long clubId, CreatePlayerDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Players.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerCreatedDto>)(await response.Content.ReadFromJsonAsync<PlayerCreatedDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Uploads a profile photo for a player.
    /// </summary>
    /// <param name="clubId">Identifier of the club that owns the player.</param>
    /// <param name="playerId">Identifier of the player whose photo is uploaded.</param>
    /// <param name="photoStream">Stream containing the image payload.</param>
    /// <param name="contentType">Media type of the uploaded image.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Stored player photo metadata when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<PlayerPhotoDto>> UploadPlayerPhotoAsync(
        long clubId,
        long playerId,
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(photoStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", "photo.jpg");

        var response = await httpClient.PutAsync(Routes.Players.ForPlayerPhoto(clubId, playerId), content, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerPhotoDto>)(await response.Content.ReadFromJsonAsync<PlayerPhotoDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a player's profile photo when one exists.
    /// </summary>
    /// <param name="clubId">Identifier of the club that owns the player.</param>
    /// <param name="playerId">Identifier of the player whose photo is requested.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// The player photo when present, <see cref="None"/> when no photo exists, or a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<OneOf<PlayerPhotoDto, None>>> GetPlayerPhotoAsync(long clubId, long playerId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Players.ForPlayerPhoto(clubId, playerId), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return (OneOf<PlayerPhotoDto, None>)new None();
        }

        return response.IsSuccessStatusCode
            ? (ServiceResult<OneOf<PlayerPhotoDto, None>>)(OneOf<PlayerPhotoDto, None>)(await response.Content.ReadFromJsonAsync<PlayerPhotoDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Validates a bulk player import file and returns row-level validation feedback.
    /// </summary>
    /// <param name="clubId">Identifier of the club targeted by the import.</param>
    /// <param name="fileStream">Stream containing the import file.</param>
    /// <param name="fileName">Original file name used to infer content type.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Validation results when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<BulkValidateResultDto>> ValidateBulkImportAsync(
        long clubId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        // Determine content type from file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension == ".csv"
            ? "text/csv"
            : "application/octet-stream";

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync(Routes.Players.BulkImport.ForValidate(clubId), content, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<BulkValidateResultDto>)(await response.Content.ReadFromJsonAsync<BulkValidateResultDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Revalidates edited bulk-import rows before final player creation.
    /// </summary>
    /// <param name="clubId">Identifier of the club targeted by the import.</param>
    /// <param name="rows">Rows to validate after client-side edits.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Validation results when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<BulkValidateResultDto>> RevalidateBulkImportAsync(
        long clubId,
        List<PlayerImportRowDto> rows,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Players.BulkImport.ForRevalidate(clubId), rows, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<BulkValidateResultDto>)(await response.Content.ReadFromJsonAsync<BulkValidateResultDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates players in bulk from previously validated import rows.
    /// </summary>
    /// <param name="clubId">Identifier of the club targeted by the import.</param>
    /// <param name="rows">Rows approved for creation.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Bulk creation results when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<BulkImportResultDto>> BulkCreatePlayersAsync(
        long clubId,
        List<PlayerImportRowDto> rows,
        CancellationToken cancellationToken)
    {
        var request = new BulkImportPlayersRequest(rows);
        var response = await httpClient.PostAsJsonAsync(Routes.Players.BulkImport.ForImport(clubId), request, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<BulkImportResultDto>)(await response.Content.ReadFromJsonAsync<BulkImportResultDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
