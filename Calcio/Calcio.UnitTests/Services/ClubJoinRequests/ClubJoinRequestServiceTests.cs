using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.ClubJoinRequests;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;

using OneOf.Types;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.ClubJoinRequests;

public class ClubJoinRequestServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetPendingRequestForCurrentUserAsync Tests

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenOk_ReturnsDto()
    {
        // Arrange
        var expectedDto = new ClubJoinRequestDto(1, 10, 100, RequestStatus.Pending);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedDto));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var dto = result.AsT0;
        dto.ClubJoinRequestId.ShouldBe(expectedDto.ClubJoinRequestId);
        dto.ClubId.ShouldBe(expectedDto.ClubId);
        dto.RequestingUserId.ShouldBe(expectedDto.RequestingUserId);
        dto.Status.ShouldBe(expectedDto.Status);
    }

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task GetPendingRequestForCurrentUserAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsT3.ShouldBeTrue();
        result.AsT3.ShouldBeOfType<Error>();
    }

    #endregion

    #region CreateJoinRequestAsync Tests

    [Fact]
    public async Task CreateJoinRequestAsync_WhenCreated_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/club-join-requests/{clubId}")
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenClubNotFound_ReturnsNotFound()
    {
        // Arrange
        var clubId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/club-join-requests/{clubId}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenConflict_ReturnsConflict()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/club-join-requests/{clubId}")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Conflict>();
    }

    [Fact]
    public async Task CreateJoinRequestAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/club-join-requests/{clubId}")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT3.ShouldBeTrue();
        result.AsT3.ShouldBeOfType<Unauthorized>();
    }

    #endregion

    #region CancelJoinRequestAsync Tests

    [Fact]
    public async Task CancelJoinRequestAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task CancelJoinRequestAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task CancelJoinRequestAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/club-join-requests/pending")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Unauthorized>();
    }

    #endregion

    #region GetPendingRequestsForClubAsync Tests

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<ClubJoinRequestWithUserDto>
        {
            new(1, clubId, 100, "John Doe", "john@example.com", RequestStatus.Pending, DateTimeOffset.UtcNow),
            new(2, clubId, 101, "Jane Doe", "jane@example.com", RequestStatus.Pending, DateTimeOffset.UtcNow)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/join-requests")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var list = result.AsT0;
        list.Count.ShouldBe(2);
        list[0].RequestingUserFullName.ShouldBe("John Doe");
        list[1].RequestingUserFullName.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/join-requests")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<ClubJoinRequestWithUserDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/join-requests")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/join-requests")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Error>();
    }

    #endregion

    #region ApproveJoinRequestAsync Tests

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/approve")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.ApproveJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/approve")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.ApproveJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/approve")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.ApproveJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Unauthorized>();
    }

    #endregion

    #region RejectJoinRequestAsync Tests

    [Fact]
    public async Task RejectJoinRequestAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/reject")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.RejectJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/reject")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.RejectJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/reject")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.RejectJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task RejectJoinRequestAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/join-requests/{requestId}/reject")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestService(httpClient);

        // Act
        var result = await service.RejectJoinRequestAsync(clubId, requestId, CancellationToken.None);

        // Assert
        result.IsT3.ShouldBeTrue();
        result.AsT3.ShouldBeOfType<Error>();
    }

    #endregion
}
