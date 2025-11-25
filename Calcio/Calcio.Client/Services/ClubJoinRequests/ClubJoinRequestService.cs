using System.Net.Http.Json;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using OneOf;
using OneOf.Types;

namespace Calcio.Client.Services.ClubJoinRequests;

public class ClubJoinRequestService(HttpClient httpClient) : IClubJoinRequestService
{
    public async Task<OneOf<ClubJoinRequestDto, NotFound, Unauthorized, Error>> GetPendingRequestForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("api/club-join-requests/pending", cancellationToken);

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<ClubJoinRequestDto>(cancellationToken) ?? throw new InvalidOperationException("Response body was null"),
            System.Net.HttpStatusCode.Unauthorized => new Unauthorized(),
            System.Net.HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Conflict, Unauthorized, Error>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/club-join-requests/{clubId}", null, cancellationToken);

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Created => new Success(),
            System.Net.HttpStatusCode.Unauthorized => new Unauthorized(),
            System.Net.HttpStatusCode.NotFound => new NotFound(),
            System.Net.HttpStatusCode.Conflict => new Conflict(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync("api/club-join-requests/pending", cancellationToken);

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.NoContent => new Success(),
            System.Net.HttpStatusCode.Unauthorized => new Unauthorized(),
            System.Net.HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }
}
