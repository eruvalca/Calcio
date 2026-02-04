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

public class PlayersService(HttpClient httpClient) : IPlayersService
{
    public async Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Players.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<List<ClubPlayerDto>>)(await response.Content.ReadFromJsonAsync<List<ClubPlayerDto>>(cancellationToken) ?? [])
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<PlayerCreatedDto>> CreatePlayerAsync(long clubId, CreatePlayerDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Players.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerCreatedDto>)(await response.Content.ReadFromJsonAsync<PlayerCreatedDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

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
        var contentType = extension switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync(Routes.Players.BulkImport.ForValidate(clubId), content, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<BulkValidateResultDto>)(await response.Content.ReadFromJsonAsync<BulkValidateResultDto>(cancellationToken))!
            : await response.ToServiceProblemAsync(cancellationToken);
    }

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
