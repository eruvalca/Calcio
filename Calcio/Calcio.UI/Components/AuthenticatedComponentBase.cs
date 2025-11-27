using System.Security.Claims;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Calcio.UI.Components;

/// <summary>
/// Base class for components that require access to the authenticated user's identity.
/// Inherits from <see cref="CancellableComponentBase"/> to provide cancellation support.
/// </summary>
public abstract class AuthenticatedComponentBase : CancellableComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private long? _currentUserId;

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user is not authenticated or the user ID claim is missing.
    /// Access this property in OnParametersSetAsync or later lifecycle methods.
    /// </exception>
    protected long CurrentUserId
        => _currentUserId
            ?? throw new InvalidOperationException(
                "CurrentUserId is not available. Ensure the component has rendered " +
                "and the user is authenticated. Access this property in OnParametersSetAsync or later.");

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    protected bool IsAuthenticated => _currentUserId.HasValue;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _currentUserId = long.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
