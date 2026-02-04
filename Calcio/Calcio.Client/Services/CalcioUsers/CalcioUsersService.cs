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

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ClubMemberDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubMembers.ForMember(clubId, userId), cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
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
            : await response.ToServiceProblemAsync(cancellationToken);
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
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<bool>> HasAccountPhotoAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Account.ForPhoto(), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return false;
        }

        return response.IsSuccessStatusCode
            ? true
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
