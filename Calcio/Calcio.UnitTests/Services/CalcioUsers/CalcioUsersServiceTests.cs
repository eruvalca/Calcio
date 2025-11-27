using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.CalcioUsers;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;

using OneOf.Types;

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
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/members")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        var list = result.AsT0;
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
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/members")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<ClubMemberDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/members")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task GetClubMembersAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/members")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetClubMembersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Error>();
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
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/clubs/{clubId}/members/{userId}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var clubId = 10L;
        var userId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/clubs/{clubId}/members/{userId}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var clubId = 10L;
        var userId = 100L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/clubs/{clubId}/members/{userId}")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsT2.ShouldBeTrue();
        result.AsT2.ShouldBeOfType<Unauthorized>();
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenServerError_ReturnsError()
    {
        // Arrange
        var clubId = 10L;
        var userId = 100L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"{BaseUrl}/api/clubs/{clubId}/members/{userId}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.RemoveClubMemberAsync(clubId, userId, CancellationToken.None);

        // Assert
        result.IsT3.ShouldBeTrue();
        result.AsT3.ShouldBeOfType<Error>();
    }

    #endregion
}
