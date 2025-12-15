using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Seasons;
using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Seasons;

public class SeasonServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetSeasonsAsync Tests

    [Fact]
    public async Task GetSeasonsAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<SeasonDto>
        {
            new(1, "2024-2025", new DateOnly(2024, 8, 1), new DateOnly(2025, 5, 31), true),
            new(2, "2025-2026", new DateOnly(2025, 8, 1), null, false)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].Name.ShouldBe("2024-2025");
        list[0].IsComplete.ShouldBeTrue();
        list[1].Name.ShouldBe("2025-2026");
        list[1].IsComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<SeasonDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    [Fact]
    public async Task GetSeasonsAsync_WhenNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSeasonsAsync_CorrectlyMapsSeasonProperties()
    {
        // Arrange
        var clubId = 10L;
        var startDate = new DateOnly(2024, 8, 15);
        var endDate = new DateOnly(2025, 5, 20);
        var expectedSeason = new SeasonDto(42, "Test Season", startDate, endDate, true);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<SeasonDto> { expectedSeason }));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.GetSeasonsAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var seasons = result.Value;
        seasons.Count.ShouldBe(1);

        var season = seasons[0];
        season.SeasonId.ShouldBe(42);
        season.Name.ShouldBe("Test Season");
        season.StartDate.ShouldBe(startDate);
        season.EndDate.ShouldBe(endDate);
        season.IsComplete.ShouldBeTrue();
    }

    #endregion

    #region CreateSeasonAsync Tests

    [Fact]
    public async Task CreateSeasonAsync_WhenCreated_ReturnsSuccess()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateSeasonDto("Spring 2025", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddMonths(3)));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSeasonAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateSeasonDto("Spring 2025", DateOnly.FromDateTime(DateTime.Today));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task CreateSeasonAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateSeasonDto("Spring 2025", DateOnly.FromDateTime(DateTime.Today));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    public async Task CreateSeasonAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreateSeasonDto("Spring 2025", DateOnly.FromDateTime(DateTime.Today));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    [Fact]
    public async Task CreateSeasonAsync_WithEndDate_SendsCorrectPayload()
    {
        // Arrange
        var clubId = 10L;
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var endDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));
        var dto = new CreateSeasonDto("Fall 2025", startDate, endDate);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .With(request =>
            {
                var content = request.Content?.ReadAsStringAsync().Result;
                return content is not null &&
                       content.Contains("Fall 2025") &&
                       content.Contains(startDate.ToString("yyyy-MM-dd")) &&
                       content.Contains(endDate.ToString("yyyy-MM-dd"));
            })
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSeasonAsync_WithoutEndDate_SendsNullEndDate()
    {
        // Arrange
        var clubId = 10L;
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var dto = new CreateSeasonDto("Spring 2025", startDate);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/{Routes.Seasons.ForClub(clubId)}")
            .With(request =>
            {
                var content = request.Content?.ReadAsStringAsync().Result;
                return content is not null &&
                       content.Contains("Spring 2025") &&
                       content.Contains("\"endDate\":null");
            })
            .Respond(HttpStatusCode.Created);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new SeasonsService(httpClient);

        // Act
        var result = await service.CreateSeasonAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
