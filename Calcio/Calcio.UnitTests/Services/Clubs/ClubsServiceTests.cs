using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Clubs;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Clubs;

public class ClubsServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetUserClubsAsync Tests

    [Fact]
    public async Task GetUserClubsAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var expectedList = new List<BaseClubDto>
        {
            new(1, "FC Barcelona", "Barcelona", "CA"),
            new(2, "Real Madrid", "Madrid", "TX")
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetUserClubsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("FC Barcelona");
        list[1].Name.ShouldBe("Real Madrid");
    }

    [Fact]
    public async Task GetUserClubsAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<BaseClubDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetUserClubsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserClubsAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetUserClubsAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetUserClubsAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetUserClubsAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region GetAllClubsForBrowsingAsync Tests

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var expectedList = new List<BaseClubDto>
        {
            new(1, "FC Barcelona", "Barcelona", "CA"),
            new(2, "Real Madrid", "Madrid", "TX"),
            new(3, "Manchester United", "Manchester", "NY")
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForBrowsing()}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForBrowsing()}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<BaseClubDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForBrowsing()}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetAllClubsForBrowsingAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForBrowsing()}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetAllClubsForBrowsingAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region GetClubByIdAsync Tests

    [Fact]
    public async Task GetClubByIdAsync_WhenOk_ReturnsClub()
    {
        // Arrange
        var clubId = 10L;
        var expectedClub = new BaseClubDto(clubId, "FC Barcelona", "Barcelona", "CA");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedClub));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetClubByIdAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var club = result.Value;
        club.Id.ShouldBe(clubId);
        club.Name.ShouldBe("FC Barcelona");
        club.City.ShouldBe("Barcelona");
        club.State.ShouldBe("CA");
    }

    [Fact]
    public async Task GetClubByIdAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForClub(clubId)}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetClubByIdAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task GetClubByIdAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetClubByIdAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetClubByIdAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Clubs.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.GetClubByIdAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region CreateClubAsync Tests

    [Fact]
    public async Task CreateClubAsync_WhenCreated_ReturnsClubCreatedDto()
    {
        // Arrange
        var dto = new CreateClubDto("FC Barcelona", "Barcelona", "CA");
        var expectedResponse = new ClubCreatedDto(1, "FC Barcelona");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.Created, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.CreateClubAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var created = result.Value;
        created.ClubId.ShouldBe(1);
        created.Name.ShouldBe("FC Barcelona");
    }

    [Fact]
    public async Task CreateClubAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var dto = new CreateClubDto("FC Barcelona", "Barcelona", "CA");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.CreateClubAsync(dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task CreateClubAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var dto = new CreateClubDto("FC Barcelona", "Barcelona", "CA");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.CreateClubAsync(dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    public async Task CreateClubAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var dto = new CreateClubDto("FC Barcelona", "Barcelona", "CA");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.CreateClubAsync(dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task CreateClubAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var dto = new CreateClubDto("FC Barcelona", "Barcelona", "CA");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Clubs.Base}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new ClubsService(httpClient);

        // Act
        var result = await service.CreateClubAsync(dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion
}
