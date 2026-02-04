using System.Net;
using System.Text.Json;

using Calcio.Shared.Results;

using Shouldly;

namespace Calcio.UnitTests.Results;

public class HttpResponseMessageExtensionsTests
{
    #region Status Code Mapping Tests

    [Fact]
    public async Task ToServiceProblemAsync_WhenNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.NotFound);
        result.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.Forbidden);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.Forbidden);
        result.StatusCode.ShouldBe(403);
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenConflict_ReturnsConflictProblem()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.Conflict);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.Conflict);
        result.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenBadRequest_ReturnsBadRequestProblem()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.BadRequest);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.BadRequest);
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenInternalServerError_ReturnsServerErrorProblem()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.InternalServerError);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.ServerError);
        result.StatusCode.ShouldBe(500);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.NotImplemented)]
    public async Task ToServiceProblemAsync_WhenUnmappedErrorStatus_ReturnsServerErrorProblem(HttpStatusCode statusCode)
    {
        // Arrange
        var response = CreateResponse(statusCode);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.ServerError);
    }

    #endregion

    #region ProblemDetails Extraction Tests

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsProblemDetails_ExtractsDetail()
    {
        // Arrange
        var problemDetails = new { detail = "The file format is not supported." };
        var response = CreateResponseWithJson(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.BadRequest);
        result.Detail.ShouldBe("The file format is not supported.");
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsFullProblemDetails_ExtractsDetail()
    {
        // Arrange - full RFC 7807 ProblemDetails format
        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = 400,
            detail = "Validation failed for the request.",
            traceId = "00-abc123-def456-00"
        };
        var response = CreateResponseWithJson(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Detail.ShouldBe("Validation failed for the request.");
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsEmptyBody_ReturnsNullDetail()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsNonJsonBody_ReturnsNullDetail()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.BadRequest);
        response.Content = new StringContent("Plain text error message");

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.BadRequest);
        result.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsInvalidJson_ReturnsNullDetail()
    {
        // Arrange
        var response = CreateResponse(HttpStatusCode.BadRequest);
        response.Content = new StringContent("{ invalid json }");

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.BadRequest);
        result.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task ToServiceProblemAsync_WhenResponseContainsJsonWithoutDetail_ReturnsNullDetail()
    {
        // Arrange
        var jsonWithoutDetail = new { title = "Error", status = 400 };
        var response = CreateResponseWithJson(HttpStatusCode.BadRequest, jsonWithoutDetail);

        // Act
        var result = await response.ToServiceProblemAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Kind.ShouldBe(ServiceProblemKind.BadRequest);
        result.Detail.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode) => new(statusCode);

    private static HttpResponseMessage CreateResponseWithJson(HttpStatusCode statusCode, object content)
    {
        var json = JsonSerializer.Serialize(content);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    #endregion
}
