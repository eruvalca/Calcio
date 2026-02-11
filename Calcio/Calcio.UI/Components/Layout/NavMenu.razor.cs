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

public partial class NavMenu(
    NavigationManager navigationManager,
    ICalcioUsersService calcioUsersService,
    IClubsService clubsService,
    ThemeService themeService,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<NavMenu> logger)
{
    private string? currentUrl;
    private bool _themeSubscribed;
    private bool _authSubscribed;
    private string? _currentUserId;
    private bool _pendingAuthRefresh;

    [PersistentState]
    public List<BaseClubDto>? UserClubs { get; set; }

    [PersistentState]
    public string? UserPhotoUrl { get; set; }

    private bool IsLoadingPhoto { get; set; } = true;

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

    private void OnThemeChanged(ThemePreference pref) => InvokeAsync(StateHasChanged);

    private string IsActive(ThemePreference pref) => themeService.Current == pref ? "active" : string.Empty;

    private string GetThemeLabel() => themeService.Current switch
    {
        ThemePreference.Light => "☀️",
        ThemePreference.Dark => "🌙",
        _ => "🌗"
    };

    private async Task ChangeTheme(ThemePreference pref) => await themeService.SetThemeAsync(pref);

    private void HandleAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
        => _ = HandleAuthenticationStateChangedAsync(authStateTask);

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

    private async Task RefreshUserClubsAsync()
    {
        var result = await clubsService.GetUserClubsAsync(CancellationToken);
        if (result.IsSuccess)
        {
            UserClubs = result.Value;
        }
    }

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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = navigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
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

    [LoggerMessage(1, LogLevel.Warning, "Authentication state change handling failed.")]
    private static partial void LogAuthStateChangedFailed(ILogger logger, Exception exception);
}
