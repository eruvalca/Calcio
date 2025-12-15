using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

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
}
