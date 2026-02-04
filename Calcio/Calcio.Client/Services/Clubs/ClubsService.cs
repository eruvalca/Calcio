using System.Net.Http.Json;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Clubs;

using OneOf.Types;

namespace Calcio.Client.Services.Clubs;

public class ClubsService(HttpClient httpClient) : IClubsService
{
    public async Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.Base, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<List<BaseClubDto>>> GetAllClubsForBrowsingAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.ForBrowsing(), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<BaseClubDto>>(cancellationToken) ?? []
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<ClubCreatedDto>> CreateClubAsync(CreateClubDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(Routes.Clubs.Base, dto, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ClubCreatedDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<BaseClubDto>> GetClubByIdAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(Routes.Clubs.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<BaseClubDto>(cancellationToken)
                ?? throw new InvalidOperationException("Response body was null")
            : await response.ToServiceProblemAsync(cancellationToken);
    }

    public async Task<ServiceResult<Success>> LeaveClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync(Routes.ClubMembership.ForClub(clubId), cancellationToken);

        return response.IsSuccessStatusCode
            ? new Success()
            : await response.ToServiceProblemAsync(cancellationToken);
    }
}
