using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.Players;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.Players;

public class PlayersServiceTests
{
    private const string BaseUrl = "http://localhost";

    #region GetClubPlayersAsync Tests

    [Fact]
    public async Task GetClubPlayersAsync_WhenOk_ReturnsList()
    {
        // Arrange
        var clubId = 10L;
        var expectedList = new List<ClubPlayerDto>
        {
            new(1, "John", "Doe", "John Doe", DateOnly.FromDateTime(DateTime.Today.AddYears(-15)), Gender.Male, 10, 100),
            new(2, "Jane", "Smith", "Jane Smith", DateOnly.FromDateTime(DateTime.Today.AddYears(-14)), Gender.Female, 7, null)
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedList));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetClubPlayersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var list = result.Value;
        list.Count.ShouldBe(2);
        list[0].FirstName.ShouldBe("John");
        list[0].LastName.ShouldBe("Doe");
        list[1].FirstName.ShouldBe("Jane");
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new List<ClubPlayerDto>()));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetClubPlayersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetClubPlayersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetClubPlayersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetClubPlayersAsync(clubId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region CreatePlayerAsync Tests

    [Fact]
    public async Task CreatePlayerAsync_WhenCreated_ReturnsPlayerCreatedDto()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3,
            Gender: Gender.Male,
            JerseyNumber: 10,
            TryoutNumber: 100);

        var expectedResponse = new PlayerCreatedDto(1, "Test", "Player", "Test Player");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.Created, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var created = result.Value;
        created.PlayerId.ShouldBe(1);
        created.FirstName.ShouldBe("Test");
        created.LastName.ShouldBe("Player");
        created.FullName.ShouldBe("Test Player");
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.Conflict);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Conflict);
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenBadRequest_ReturnsBadRequestProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.BadRequest);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.BadRequest);
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var dto = new CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.CreatePlayerAsync(clubId, dto, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region UploadPlayerPhotoAsync Tests

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenOk_ReturnsPlayerPhotoDto()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;
        var expectedResponse = new PlayerPhotoDto(
            1,
            "https://storage.blob.core.windows.net/photos/original.jpg?sas",
            "https://storage.blob.core.windows.net/photos/small.jpg?sas",
            "https://storage.blob.core.windows.net/photos/medium.jpg?sas",
            "https://storage.blob.core.windows.net/photos/large.jpg?sas");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var photo = result.Value;
        photo.PlayerPhotoId.ShouldBe(1);
        photo.OriginalUrl.ShouldContain("original.jpg");
        photo.SmallUrl!.ShouldContain("small.jpg");
        photo.MediumUrl!.ShouldContain("medium.jpg");
        photo.LargeUrl!.ShouldContain("large.jpg");
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenBadRequest_ReturnsBadRequestProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.BadRequest);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.BadRequest);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadPlayerPhotoAsync(clubId, playerId, photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region GetPlayerPhotoAsync Tests

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenOk_ReturnsPlayerPhotoDto()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;
        var expectedResponse = new PlayerPhotoDto(
            1,
            "https://storage.blob.core.windows.net/photos/original.jpg?sas",
            "https://storage.blob.core.windows.net/photos/small.jpg?sas",
            "https://storage.blob.core.windows.net/photos/medium.jpg?sas",
            "https://storage.blob.core.windows.net/photos/large.jpg?sas");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT0.ShouldBeTrue(); // PlayerPhotoDto
        var photo = result.Value.AsT0;
        photo.PlayerPhotoId.ShouldBe(1);
        photo.OriginalUrl.ShouldContain("original.jpg");
    }

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenNoContent_ReturnsNone()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT1.ShouldBeTrue(); // None
    }

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 999L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var clubId = 10L;
        var playerId = 1L;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/api/clubs/{clubId}/players/{playerId}/photo")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new PlayersService(httpClient);

        // Act
        var result = await service.GetPlayerPhotoAsync(clubId, playerId, CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion
}
