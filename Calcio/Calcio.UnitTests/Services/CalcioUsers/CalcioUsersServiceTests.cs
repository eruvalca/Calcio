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

    #region UploadAccountPhotoAsync Tests

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
