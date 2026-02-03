using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.Players;
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
            : (ServiceResult<List<ClubPlayerDto>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<PlayerCreatedDto>> CreatePlayerAsync(long clubId, CreatePlayerDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Players.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerCreatedDto>)(await response.Content.ReadFromJsonAsync<PlayerCreatedDto>(cancellationToken))!
            : (ServiceResult<PlayerCreatedDto>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.Conflict => ServiceProblem.Conflict(),
                HttpStatusCode.BadRequest => ServiceProblem.BadRequest(),
                _ => ServiceProblem.ServerError()
            });
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
            : (ServiceResult<PlayerPhotoDto>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.BadRequest => ServiceProblem.BadRequest(),
                _ => ServiceProblem.ServerError()
            });
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
            : (ServiceResult<OneOf<PlayerPhotoDto, None>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<PlayerImportResultDto>> BulkImportPlayersAsync(
        long clubId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync(Routes.Players.ForBulkImport(clubId), content, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerImportResultDto>)(await response.Content.ReadFromJsonAsync<PlayerImportResultDto>(cancellationToken))!
            : (ServiceResult<PlayerImportResultDto>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.BadRequest => ServiceProblem.BadRequest(await response.Content.ReadAsStringAsync(cancellationToken)),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<PlayerImportStatusDto>> GetImportStatusAsync(long clubId, long importId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Players.ForImportStatus(clubId, importId), cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<PlayerImportStatusDto>)(await response.Content.ReadFromJsonAsync<PlayerImportStatusDto>(cancellationToken))!
            : (ServiceResult<PlayerImportStatusDto>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                _ => ServiceProblem.ServerError()
            });
    }

    public Stream GenerateImportTemplate()
    {
        // For client-side, we'll download the template from the server
        throw new NotSupportedException("Template generation is only supported on the server side. Use the template download endpoint instead.");
    }
}
