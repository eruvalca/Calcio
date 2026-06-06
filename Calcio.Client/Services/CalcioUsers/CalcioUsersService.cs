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

/// <summary>
/// Provides client-side operations for club-member management and account photo endpoints.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class CalcioUsersService(HttpClient httpClient) : ICalcioUsersService
{
    /// <summary>
    /// Retrieves members that belong to the specified club.
    /// </summary>
    /// <param name="clubId">Identifier of the club whose members are requested.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of club members when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.ClubMembers.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ClubMemberDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Removes a member from the specified club.
    /// </summary>
    /// <param name="clubId">Identifier of the club from which the member is removed.</param>
    /// <param name="userId">Identifier of the user to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when removal succeeds; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubMembers.ForMember(clubId, userId), cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Uploads the authenticated user's profile photo.
    /// </summary>
    /// <param name="photoStream">Stream containing the photo payload.</param>
    /// <param name="contentType">Media type of the uploaded image.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// The stored photo metadata when successful; otherwise a mapped service problem.
    /// </returns>
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

    /// <summary>
    /// Gets the authenticated user's photo when one exists.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// The user photo when present, <see cref="None"/> when no photo exists, or a mapped service problem.
    /// </returns>
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

    /// <summary>
    /// Checks whether the authenticated user currently has a stored profile photo.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// <see langword="true"/> when a photo exists, <see langword="false"/> when none exists, or a mapped service problem.
    /// </returns>
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
