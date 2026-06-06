using System.Net.Http.Json;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Seasons;

using OneOf.Types;

namespace Calcio.Client.Services.Seasons;

/// <summary>
/// Provides client-side season management operations for clubs.
/// </summary>
/// <param name="httpClient">HTTP client configured for authenticated API calls.</param>
public class SeasonsService(HttpClient httpClient) : ISeasonsService
{
    /// <summary>
    /// Retrieves all seasons configured for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club whose seasons are requested.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A list of seasons when successful; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<List<SeasonDto>>> GetSeasonsAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Seasons.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<SeasonDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new season for a club.
    /// </summary>
    /// <param name="clubId">Identifier of the club where the season is created.</param>
    /// <param name="dto">Payload describing the season to create.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>
    /// A successful result when creation succeeds; otherwise a mapped service problem.
    /// </returns>
    public async Task<ServiceResult<Success>> CreateSeasonAsync(long clubId, CreateSeasonDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Seasons.ForClub(clubId), dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
