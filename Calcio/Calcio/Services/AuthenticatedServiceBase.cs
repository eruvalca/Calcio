using System.Security.Claims;

namespace Calcio.Services;

/// <summary>
/// Base class for services that require access to the authenticated user's identity.
/// Provides a common way to retrieve the current user's ID from the HTTP context.
/// </summary>
public abstract class AuthenticatedServiceBase(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// Gets the current authenticated user's ID from the HTTP context claims.
    /// </summary>
    /// <returns>The user ID if authenticated.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated or the user ID claim is missing or invalid.</exception>
    protected long CurrentUserId
        => long.TryParse(httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException();
}
