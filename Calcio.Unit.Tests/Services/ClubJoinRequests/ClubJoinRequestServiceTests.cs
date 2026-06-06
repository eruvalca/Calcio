using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.ClubJoinRequests;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.ClubJoinRequests;

/// <summary>
/// Contains unit tests for C lu bJ oi nR eq ue st Se rv ic e behavior.
/// </summary>
public class ClubJoinRequestServiceTests
{
    /// <summary>
    /// Defines the base URL used by mocked HTTP requests in this test class.
    /// </summary>
    private const string BaseUrl = "http://localhost";

    #region GetRequestForCurrentUserAsync Tests
    /// <summary>
    /// Verifies the GetRequestForCurrentUserAsync_WhenOk_ReturnsDto scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetRequestForCurrentUserAsync_WhenOk_ReturnsDto()
    {
        // Arrange
        var expectedDto = new ClubJoinRequestDto(1, 10, 100, RequestStatus.Pending);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.GetCurrent}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedDto));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.ClubJoinRequestId.ShouldBe(expectedDto.ClubJoinRequestId);
        dto.ClubId.ShouldBe(expectedDto.ClubId);
        dto.RequestingUserId.ShouldBe(expectedDto.RequestingUserId);
        dto.Status.ShouldBe(expectedDto.Status);
    }
    /// <summary>
    /// Verifies the GetRequestForCurrentUserAsync_WhenNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetRequestForCurrentUserAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.GetCurrent}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }
    /// <summary>
    /// Verifies the GetRequestForCurrentUserAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetRequestForCurrentUserAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.GetCurrent}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the GetRequestForCurrentUserAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetRequestForCurrentUserAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.GetCurrent}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetRequestForCurrentUserAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region CreateJoinRequestAsync Tests
    /// <summary>
    /// Verifies the CreateJoinRequestAsync_WhenCreated_ReturnsSuccess scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CreateJoinRequestAsync_WhenCreated_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.ClubJoinRequests.ForClub(clubId)}")
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
    /// <summary>
    /// Verifies the CreateJoinRequestAsync_WhenClubNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CreateJoinRequestAsync_WhenClubNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.ClubJoinRequests.ForClub(clubId)}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }
    /// <summary>
    /// Verifies the CreateJoinRequestAsync_WhenConflict_ReturnsConflictProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CreateJoinRequestAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.ClubJoinRequests.ForClub(clubId)}")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }
    /// <summary>
    /// Verifies the CreateJoinRequestAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CreateJoinRequestAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.ClubJoinRequests.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CreateJoinRequestAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    #endregion

    #region CancelJoinRequestAsync Tests
    /// <summary>
    /// Verifies the CancelJoinRequestAsync_WhenNoContent_ReturnsSuccess scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CancelJoinRequestAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubJoinRequests.CancelCurrent}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
    /// <summary>
    /// Verifies the CancelJoinRequestAsync_WhenNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CancelJoinRequestAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubJoinRequests.CancelCurrent}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }
    /// <summary>
    /// Verifies the CancelJoinRequestAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task CancelJoinRequestAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubJoinRequests.CancelCurrent}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.CancelJoinRequestAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    #endregion

    #region GetPendingRequestsForClubAsync Tests
    /// <summary>
    /// Verifies the GetPendingRequestsForClubAsync_WhenOk_ReturnsList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].RequestingUserFullName.ShouldBe("John Doe");
        list[1].RequestingUserFullName.ShouldBe("Jane Doe");
    }
    /// <summary>
    /// Verifies the GetPendingRequestsForClubAsync_WhenEmptyResponse_ReturnsEmptyList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<ClubJoinRequestWithUserDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
    /// <summary>
    /// Verifies the GetPendingRequestsForClubAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the GetPendingRequestsForClubAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetPendingRequestsForClubAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.GetPendingRequestsForClubAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region UpdateJoinRequestStatusAsync Tests
    /// <summary>
    /// Verifies the UpdateJoinRequestStatusAsync_WhenNoContent_ReturnsSuccess scenario.
    /// </summary>
    /// <param name="status">The status value used by this test scenario.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task UpdateJoinRequestStatusAsync_WhenNoContent_ReturnsSuccess(RequestStatus status)
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId)}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, status, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
    /// <summary>
    /// Verifies the UpdateJoinRequestStatusAsync_WhenNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <param name="status">The status value used by this test scenario.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task UpdateJoinRequestStatusAsync_WhenNotFound_ReturnsNotFoundProblem(RequestStatus status)
    {
        // Arrange
        var clubId = 10L;
        var requestId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId)}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, status, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }
    /// <summary>
    /// Verifies the UpdateJoinRequestStatusAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <param name="status">The status value used by this test scenario.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task UpdateJoinRequestStatusAsync_WhenForbidden_ReturnsForbiddenProblem(RequestStatus status)
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, status, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the UpdateJoinRequestStatusAsync_WhenBadRequest_ReturnsBadRequestProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task UpdateJoinRequestStatusAsync_WhenBadRequest_ReturnsBadRequestProblem()
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId)}")
            .Respond(HttpStatusCode.BadRequest);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, RequestStatus.Pending, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.BadRequest);
    }
    /// <summary>
    /// Verifies the UpdateJoinRequestStatusAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <param name="status">The status value used by this test scenario.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task UpdateJoinRequestStatusAsync_WhenServerError_ReturnsServerErrorProblem(RequestStatus status)
    {
        // Arrange
        var clubId = 10L;
        var requestId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Patch, $"{BaseUrl}/{Routes.ClubJoinRequests.Admin.ForRequest(clubId, requestId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubJoinRequestsService(httpClient);

        // Act
        var result = await service.UpdateJoinRequestStatusAsync(clubId, requestId, status, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion
}
