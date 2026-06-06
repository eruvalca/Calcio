namespace Calcio.Shared.Results;

/// <summary>
/// Represents a known problem from a service operation.
/// Maps directly to HTTP status codes and ProblemDetails for API responses.
/// </summary>
public readonly record struct ServiceProblem(ServiceProblemKind Kind, string? Detail = null)
{
    /// <summary>
    /// Gets the HTTP status code corresponding to this problem kind.
    /// </summary>
    public int StatusCode => Kind switch
    {
        ServiceProblemKind.NotFound => 404,
        ServiceProblemKind.Forbidden => 403,
        ServiceProblemKind.Conflict => 409,
        ServiceProblemKind.BadRequest => 400,
        _ => 500
    };

    /// <summary>
    /// Creates a NotFound problem (HTTP 404).
    /// </summary>
    public static ServiceProblem NotFound(string? detail = null) => new(ServiceProblemKind.NotFound, detail);

    /// <summary>
    /// Creates a Forbidden problem (HTTP 403).
    /// Use this when the user is authenticated but not authorized for the operation.
    /// </summary>
    public static ServiceProblem Forbidden(string? detail = null) => new(ServiceProblemKind.Forbidden, detail);

    /// <summary>
    /// Creates a Conflict problem (HTTP 409).
    /// </summary>
    public static ServiceProblem Conflict(string? detail = null) => new(ServiceProblemKind.Conflict, detail);

    /// <summary>
    /// Creates a BadRequest problem (HTTP 400).
    /// </summary>
    public static ServiceProblem BadRequest(string? detail = null) => new(ServiceProblemKind.BadRequest, detail);

    /// <summary>
    /// Creates a ServerError problem (HTTP 500).
    /// </summary>
    public static ServiceProblem ServerError(string? detail = null) => new(ServiceProblemKind.ServerError, detail);
}

/// <summary>
/// Defines the kinds of problems that can occur in service operations.
/// Note: 401 Unauthorized is not included because authentication failures are handled
/// at the middleware/endpoint authorization layer, not in service operations.
/// </summary>
public enum ServiceProblemKind
{
    /// <summary>The requested resource was not found.</summary>
    NotFound,

    /// <summary>The user is authenticated but not authorized for this operation.</summary>
    Forbidden,

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    Conflict,

    /// <summary>The request was invalid or malformed.</summary>
    BadRequest,

    /// <summary>An unexpected server error occurred.</summary>
    ServerError
}
