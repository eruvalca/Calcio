using System.Net;
using System.Net.Http.Json;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.CalcioUsers;

public class CalcioUsersService(HttpClient httpClient) : ICalcioUsersService
{
    public async Task<OneOf<List<ClubMemberDto>, Unauthorized, Error>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/members", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<ClubMemberDto>>(cancellationToken) ?? [],
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"api/clubs/{clubId}/members/{userId}", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => new Success(),
            HttpStatusCode.NotFound => new NotFound(),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }
}
