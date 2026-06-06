using System.Security.Claims;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Calcio.UI.Components.Layout;

/// <summary>
/// Renders the primary navigation menu and keeps user navigation, clubs, profile photo, and theme state synchronized.
/// </summary>
/// <param name="navigationManager">The navigation manager used for URL state and refresh operations.</param>
/// <param name="calcioUsersService">The service used to retrieve the signed-in user's photo.</param>
/// <param name="clubsService">The service used to retrieve clubs available to the signed-in user.</param>
/// <param name="themeService">The service used to initialize and change theme preference.</param>
/// <param name="authenticationStateProvider">The provider used to observe authentication state changes.</param>
/// <param name="logger">The logger used to capture authentication refresh failures.</param>
public partial class NavMenu(
    NavigationManager navigationManager,
    ICalcioUsersService calcioUsersService,
    IClubsService clubsService,
    ThemeService themeService,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<NavMenu> logger)
{
    /// <summary>
    /// Stores the current relative URL used to highlight active navigation links.
    /// </summary>
    private string? currentUrl;

    /// <summary>
    /// Indicates whether the component subscribed to theme change events.
    /// </summary>
    private bool _themeSubscribed;

    /// <summary>
    /// Indicates whether the component subscribed to authentication state change events.
    /// </summary>
    private bool _authSubscribed;

    /// <summary>
    /// Tracks the authenticated user ID to detect account switches.
    /// </summary>
    private string? _currentUserId;

    /// <summary>
    /// Indicates whether another authentication check is needed once interactivity is available.
    /// </summary>
    private bool _pendingAuthRefresh;

    /// <summary>
    /// Gets or sets the clubs associated with the current user for prerender persistence.
    /// </summary>
    [PersistentState]
    public List<BaseClubDto>? UserClubs { get; set; }

    /// <summary>
    /// Gets or sets the current user's profile photo URL for prerender persistence.
    /// </summary>
    [PersistentState]
    public string? UserPhotoUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the profile photo is currently loading.
    /// </summary>
    private bool IsLoadingPhoto { get; set; } = true;

    /// <summary>
    /// Initializes navigation and authentication subscriptions, then hydrates authentication-dependent state.
    /// </summary>
    /// <returns>A task that completes when initial state loading finishes.</returns>
    protected override async Task OnInitializedAsync()
    {
        currentUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        navigationManager.LocationChanged += OnLocationChanged;

        if (!_authSubscribed)
        {
            authenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
            _authSubscribed = true;
        }

        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        await HandleAuthStateAsync(authState);
    }

    /// <summary>
    /// Completes interactive-only initialization such as theme subscription and deferred auth refresh.
    /// </summary>
    /// <param name="firstRender">A value indicating whether this is the first render pass.</param>
    /// <returns>A task that completes when after-render initialization is done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            if (!_themeSubscribed)
            {
                await themeService.InitializeAsync();
                themeService.ThemeChanged += OnThemeChanged;
                _themeSubscribed = true;
                StateHasChanged();
            }

            if (_pendingAuthRefresh)
            {
                await EnsureAuthStateHydratedAsync();
            }
        }
    }

    /// <summary>
    /// Requests a rerender when the active theme changes.
    /// </summary>
    /// <param name="pref">The updated theme preference.</param>
    private void OnThemeChanged(ThemePreference pref) => InvokeAsync(StateHasChanged);

    /// <summary>
    /// Returns the CSS class used to indicate the active theme option.
    /// </summary>
    /// <param name="pref">The theme option being evaluated.</param>
    /// <returns><c>active</c> when the option matches the current theme; otherwise, an empty string.</returns>
    private string IsActive(ThemePreference pref) => themeService.Current == pref ? "active" : string.Empty;

    /// <summary>
    /// Gets an icon representing the currently selected theme.
    /// </summary>
    /// <returns>An emoji string for light, dark, or system theme.</returns>
    private string GetThemeLabel() => themeService.Current switch
    {
        ThemePreference.Light => "☀️",
        ThemePreference.Dark => "🌙",
        _ => "🌗"
    };

    /// <summary>
    /// Changes the current theme preference.
    /// </summary>
    /// <param name="pref">The theme preference to apply.</param>
    /// <returns>A task that completes when the theme is updated.</returns>
    private async Task ChangeTheme(ThemePreference pref) => await themeService.SetThemeAsync(pref);

    /// <summary>
    /// Forwards authentication state change notifications to asynchronous handling.
    /// </summary>
    /// <param name="authStateTask">The authentication state task provided by the framework.</param>
    private void HandleAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
        => _ = HandleAuthenticationStateChangedAsync(authStateTask);

    /// <summary>
    /// Handles authentication state changes and refreshes user-scoped menu data.
    /// </summary>
    /// <param name="authStateTask">The authentication state task provided by the framework.</param>
    /// <returns>A task that completes when menu data has been refreshed.</returns>
    private async Task HandleAuthenticationStateChangedAsync(Task<AuthenticationState> authStateTask)
    {
        try
        {
            IsLoadingPhoto = true;
            var authState = await authStateTask;
            await HandleAuthStateAsync(authState);

            if (_pendingAuthRefresh && RendererInfo.IsInteractive)
            {
                await EnsureAuthStateHydratedAsync();
            }
        }
        catch (Exception ex)
        {
            LogAuthStateChangedFailed(logger, ex);
        }
        finally
        {
            IsLoadingPhoto = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Applies the provided authentication state to clubs and profile photo state.
    /// </summary>
    /// <param name="authState">The authentication state to process.</param>
    /// <returns>A task that completes when related data has been loaded.</returns>
    private async Task HandleAuthStateAsync(AuthenticationState authState)
    {
        // Guard: ensure user is authenticated AND has a valid NameIdentifier claim
        // This can be false during SSR prerender right after registration before claims are hydrated
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (authState.User.Identity?.IsAuthenticated is not true)
        {
            UserPhotoUrl = null;
            UserClubs = null;
            _currentUserId = null;
            IsLoadingPhoto = false;
            return;
        }

        if (userId is null)
        {
            _pendingAuthRefresh = true;
            UserPhotoUrl = null;
            UserClubs = null;
            IsLoadingPhoto = false;
            return;
        }

        _pendingAuthRefresh = false;

        if (_currentUserId is not null && !string.Equals(_currentUserId, userId, StringComparison.Ordinal))
        {
            UserPhotoUrl = null;
            UserClubs = null;
        }

        _currentUserId = userId;

        if (UserClubs is null || UserClubs.Count == 0)
        {
            await RefreshUserClubsAsync();
        }

        // Wrap in try-catch because HttpContext.User may differ from Blazor's AuthenticationState
        // during SSR prerender immediately after authentication changes
        if (string.IsNullOrEmpty(UserPhotoUrl))
        {
            try
            {
                await RefreshUserPhotoAsync();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
            {
                // User claims not yet available in HttpContext - fall back to no photo
                UserPhotoUrl = null;
            }
        }

        IsLoadingPhoto = false;
    }

    /// <summary>
    /// Retries authentication-dependent hydration once interactivity is available.
    /// </summary>
    /// <returns>A task that completes when hydration is retried.</returns>
    private async Task EnsureAuthStateHydratedAsync()
    {
        try
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authState.User.Identity?.IsAuthenticated is true && userId is not null)
            {
                await HandleAuthStateAsync(authState);
            }
        }
        catch (Exception ex)
        {
            LogAuthStateChangedFailed(logger, ex);
        }
    }

    /// <summary>
    /// Reloads the list of clubs associated with the current user.
    /// </summary>
    /// <returns>A task that completes when club retrieval finishes.</returns>
    private async Task RefreshUserClubsAsync()
    {
        var result = await clubsService.GetUserClubsAsync(CancellationToken);
        if (result.IsSuccess)
        {
            UserClubs = result.Value;
        }
    }

    /// <summary>
    /// Reloads the signed-in user's profile photo URL.
    /// </summary>
    /// <returns>A task that completes when photo retrieval finishes.</returns>
    private async Task RefreshUserPhotoAsync()
    {
        var result = await calcioUsersService.GetAccountPhotoAsync(CancellationToken);
        if (result.IsSuccess)
        {
            UserPhotoUrl = result.Value.Match<string?>(
                photo => photo.SmallUrl ?? photo.OriginalUrl,
                _ => null);
        }
    }

    /// <summary>
    /// Updates the active URL when navigation occurs.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">Navigation event details.</param>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = navigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    /// <summary>
    /// Unsubscribes from navigation, authentication, and theme events.
    /// </summary>
    public override void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
        if (_authSubscribed)
        {
            authenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
        }

        if (_themeSubscribed)
        {
            themeService.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose();
    }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    /// <summary>
    /// Logs failures that occur while handling authentication state change events.
    /// </summary>
    /// <param name="logger">The logger that writes the message.</param>
    /// <param name="exception">The exception raised during handling.</param>
    [LoggerMessage(1, LogLevel.Warning, "Authentication state change handling failed.")]
    private static partial void LogAuthStateChangedFailed(ILogger logger, Exception exception);
}
