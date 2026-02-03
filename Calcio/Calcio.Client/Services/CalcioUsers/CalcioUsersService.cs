using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.CalcioUsers;

public class CalcioUsersService(HttpClient httpClient) : ICalcioUsersService
{
    public async Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubMembers.ForClub(clubId), cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ClubMemberDto>>(cancellationToken) ?? [];
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubMembers.ForMember(clubId, userId), cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new Success();
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(),
            _ => ServiceProblem.ServerError()
        };
    }

    public async Task<ServiceResult<CalcioUserPhotoDto>> UploadAccountPhotoAsync(
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(photoStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", "photo.jpg");

        var response = await httpClient.PutAsync(Routes.Account.ForPhoto(), content, cancellationToken);

        return response.IsSuccessStatusCode
            ? (ServiceResult<CalcioUserPhotoDto>)(await response.Content.ReadFromJsonAsync<CalcioUserPhotoDto>(cancellationToken))!
            : (ServiceResult<CalcioUserPhotoDto>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                HttpStatusCode.BadRequest => ServiceProblem.BadRequest(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<OneOf<CalcioUserPhotoDto, None>>> GetAccountPhotoAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Account.ForPhoto(), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return (OneOf<CalcioUserPhotoDto, None>)new None();
        }

        return response.IsSuccessStatusCode
            ? (ServiceResult<OneOf<CalcioUserPhotoDto, None>>)(OneOf<CalcioUserPhotoDto, None>)(await response.Content.ReadFromJsonAsync<CalcioUserPhotoDto>(cancellationToken))!
            : (ServiceResult<OneOf<CalcioUserPhotoDto, None>>)(response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceProblem.NotFound(),
                HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
                _ => ServiceProblem.ServerError()
            });
    }

    public async Task<ServiceResult<bool>> HasAccountPhotoAsync(CancellationToken cancellationToken)
    {
        // Use a HEAD request or just check if GET returns NoContent vs Ok
        var response = await httpClient.GetAsync(Routes.Account.ForPhoto(), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(),
            _ => ServiceProblem.ServerError()
        };
    }
}
