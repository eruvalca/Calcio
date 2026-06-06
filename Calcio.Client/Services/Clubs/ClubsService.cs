using System.Net.Http.Json;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Clubs;

using OneOf.Types;

namespace Calcio.Client.Services.Clubs;

/// <summary>
/// Provides client-side access to club discovery, membership, and creation endpoints.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class ClubsService(HttpClient httpClient) : IClubsService
{
    /// <summary>
    /// Retrieves clubs the current user can access.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of accessible clubs when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.Base, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all clubs that are available for browsing.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of browsable clubs when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<BaseClubDto>>> GetAllClubsForBrowsingAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.ForBrowsing(), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new club from the provided creation payload.
    /// </summary>
    /// <param name="dto">Data used to create the new club.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// The created club metadata when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<ClubCreatedDto>> CreateClubAsync(CreateClubDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Clubs.Base, dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ClubCreatedDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a club by its identifier.
    /// </summary>
    /// <param name="clubId">Identifier of the club to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// Club details when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<BaseClubDto>> GetClubByIdAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<BaseClubDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Leaves the specified club for the current user.
    /// </summary>
    /// <param name="clubId">Identifier of the club to leave.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when membership removal succeeds; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> LeaveClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubMembership.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
