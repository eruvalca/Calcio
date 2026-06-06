using System.Net;
using System.Net.Http.Json;

using Calcio.Client.Services.CalcioUsers;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Endpoints;
using Calcio.Shared.Results;

using RichardSzalay.MockHttp;

using Shouldly;

namespace Calcio.UnitTests.Services.CalcioUsers;

/// <summary>
/// Contains unit tests for C al ci oU se rs Se rv ic e behavior.
/// </summary>
public class CalcioUsersServiceTests
{
    /// <summary>
    /// Defines the base URL used by mocked HTTP requests in this test class.
    /// </summary>
    private const string BaseUrl = "http://localhost";

    #region GetClubMembersAsync Tests
    /// <summary>
    /// Verifies the GetClubMembersAsync_WhenOk_ReturnsList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetClubMembersAsync_WhenEmptyResponse_ReturnsEmptyList scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetClubMembersAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the GetClubMembersAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the RemoveClubMemberAsync_WhenNoContent_ReturnsSuccess scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the RemoveClubMemberAsync_WhenNotFound_ReturnsNotFoundProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the RemoveClubMemberAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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
    /// <summary>
    /// Verifies the RemoveClubMemberAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

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

    #region UploadAccountPhotoAsync Tests
    /// <summary>
    /// Verifies the UploadAccountPhotoAsync_WhenOk_ReturnsCalcioUserPhotoDto scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task UploadAccountPhotoAsync_WhenOk_ReturnsCalcioUserPhotoDto()
    {
        // Arrange
        var expectedResponse = new CalcioUserPhotoDto(
            1,
            "https://storage.blob.core.windows.net/photos/original.jpg?sas",
            "https://storage.blob.core.windows.net/photos/small.jpg?sas",
            "https://storage.blob.core.windows.net/photos/medium.jpg?sas",
            "https://storage.blob.core.windows.net/photos/large.jpg?sas");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Put, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var photo = result.Value;
        photo.CalcioUserPhotoId.ShouldBe(1);
        photo.OriginalUrl.ShouldContain("original.jpg");
        photo.SmallUrl!.ShouldContain("small.jpg");
        photo.MediumUrl!.ShouldContain("medium.jpg");
        photo.LargeUrl!.ShouldContain("large.jpg");
    }
    /// <summary>
    /// Verifies the UploadAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task UploadAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Put, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the UploadAccountPhotoAsync_WhenBadRequest_ReturnsBadRequestProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task UploadAccountPhotoAsync_WhenBadRequest_ReturnsBadRequestProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Put, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.BadRequest);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.BadRequest);
    }
    /// <summary>
    /// Verifies the UploadAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task UploadAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Put, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        using var photoStream = new MemoryStream([0x00, 0x01, 0x02]);

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region GetAccountPhotoAsync Tests
    /// <summary>
    /// Verifies the GetAccountPhotoAsync_WhenOk_ReturnsCalcioUserPhotoDto scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetAccountPhotoAsync_WhenOk_ReturnsCalcioUserPhotoDto()
    {
        // Arrange
        var expectedResponse = new CalcioUserPhotoDto(
            1,
            "https://storage.blob.core.windows.net/photos/original.jpg?sas",
            "https://storage.blob.core.windows.net/photos/small.jpg?sas",
            "https://storage.blob.core.windows.net/photos/medium.jpg?sas",
            "https://storage.blob.core.windows.net/photos/large.jpg?sas");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT0.ShouldBeTrue(); // CalcioUserPhotoDto
        var photo = result.Value.AsT0;
        photo.CalcioUserPhotoId.ShouldBe(1);
        photo.OriginalUrl.ShouldContain("original.jpg");
    }
    /// <summary>
    /// Verifies the GetAccountPhotoAsync_WhenNoContent_ReturnsNone scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetAccountPhotoAsync_WhenNoContent_ReturnsNone()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT1.ShouldBeTrue(); // None
    }
    /// <summary>
    /// Verifies the GetAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the GetAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task GetAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.GetAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region HasAccountPhotoAsync Tests
    /// <summary>
    /// Verifies the HasAccountPhotoAsync_WhenOk_ReturnsTrue scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task HasAccountPhotoAsync_WhenOk_ReturnsTrue()
    {
        // Arrange
        var expectedResponse = new CalcioUserPhotoDto(1, "url", null, null, null);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.HasAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }
    /// <summary>
    /// Verifies the HasAccountPhotoAsync_WhenNoContent_ReturnsFalse scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task HasAccountPhotoAsync_WhenNoContent_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.NoContent);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.HasAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }
    /// <summary>
    /// Verifies the HasAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task HasAccountPhotoAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.Forbidden);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.HasAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }
    /// <summary>
    /// Verifies the HasAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem scenario.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>

    [Fact]
    public async Task HasAccountPhotoAsync_WhenServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BaseUrl}/{Routes.Account.ForPhoto()}")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);

        var service = new CalcioUsersService(httpClient);

        // Act
        var result = await service.HasAccountPhotoAsync(CancellationToken.None);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion
}
