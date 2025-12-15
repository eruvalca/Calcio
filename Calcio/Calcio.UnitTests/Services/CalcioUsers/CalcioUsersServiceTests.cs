using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.CalcioUsers;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.CalcioUsers;

public class CalcioUsersServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetClubMembersAsync Tests

    [Fact]
    public async Task GetClubMembersAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<ClubMemberDto>
        {
            new(1, "John Admin", "john@example.com", true),
            new(2, "Jane Member", "jane@example.com", false)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubMembers.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].FullName.ShouldBe("John Admin");
        list[0].IsClubAdmin.ShouldBeTrue();
        list[1].FullName.ShouldBe("Jane Member");
        list[1].IsClubAdmin.ShouldBeFalse();
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubMembers.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<ClubMemberDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubMembers.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.ClubMembers.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region RemoveClubMemberAsync Tests

    [Fact]
    public async Task RemoveClubMemberAsync_WhenNoContent_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;
        var userId = 100L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubMembers.ForMember(clubId, userId)}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 10L;
        var userId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubMembers.ForMember(clubId, userId)}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var userId = 100L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubMembers.ForMember(clubId, userId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var userId = 100L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/{Routes.ClubMembers.ForMember(clubId, userId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion
}
