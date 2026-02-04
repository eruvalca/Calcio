using System.Net;
using System.Net.Http.Json;

namespace Calcio.Shared.Results;

/// <summary>
/// Extension methods for converting HTTP responses to ServiceProblem.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Converts an unsuccessful HTTP response to a ServiceProblem,
    /// automatically extracting detail from ProblemDetails if present.
    /// </summary>
    public static async Task<ServiceProblem> ToServiceProblemAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var detail = await ExtractProblemDetailAsync(response, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => ServiceProblem.NotFound(detail),
            HttpStatusCode.Forbidden => ServiceProblem.Forbidden(detail),
            HttpStatusCode.Conflict => ServiceProblem.Conflict(detail),
            HttpStatusCode.BadRequest => ServiceProblem.BadRequest(detail),
            _ => ServiceProblem.ServerError(detail)
        };
    }

    private static async Task<string?> ExtractProblemDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(cancellationToken);
            return problem?.Detail;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Minimal representation of RFC 7807 ProblemDetails for deserializing API error responses.
    /// </summary>
    private sealed record ProblemDetailsDto(string? Detail);
}
