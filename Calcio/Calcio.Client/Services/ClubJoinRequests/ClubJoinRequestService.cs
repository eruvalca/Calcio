using System.Net;
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
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<ClubJoinRequestDto>(cancellationToken) ?? throw new InvalidOperationException("Response body was null"),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Conflict, Unauthorized, Error>> CreateJoinRequestAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/club-join-requests/{clubId}", null, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => new Success(),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            HttpStatusCode.NotFound => new NotFound(),
            HttpStatusCode.Conflict => new Conflict(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> CancelJoinRequestAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync("api/club-join-requests/pending", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => new Success(),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }

    public async Task<OneOf<List<ClubJoinRequestWithUserDto>, Unauthorized, Error>> GetPendingRequestsForClubAsync(long clubId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/clubs/{clubId}/join-requests", cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<ClubJoinRequestWithUserDto>>(cancellationToken) ?? [],
            HttpStatusCode.Unauthorized => new Unauthorized(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> ApproveJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/clubs/{clubId}/join-requests/{requestId}/approve", null, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => new Success(),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }

    public async Task<OneOf<Success, NotFound, Unauthorized, Error>> RejectJoinRequestAsync(long clubId, long requestId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/clubs/{clubId}/join-requests/{requestId}/reject", null, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => new Success(),
            HttpStatusCode.Unauthorized => new Unauthorized(),
            HttpStatusCode.NotFound => new NotFound(),
            _ => new Error()
        };
    }
}
