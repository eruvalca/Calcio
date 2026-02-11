using System.Security.Claims;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Services.CalcioUsers;
using Calcio.UI.Services.Clubs;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Calcio.UI.Components.Layout;

public partial class NavMenu(
    NavigationManager navigationManager,
    ICalcioUsersService calcioUsersService,
    IUserPhotoNotifications userPhotoNotifications,
    UserClubStateService userClubStateService,
    ThemeService themeService,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<NavMenu> logger)
{
    private string? currentUrl;
    private bool _themeSubscribed;
    private AuthenticationState? _authState;
    private bool _authSubscribed;
    private string? _currentUserId;
    private bool _pendingAuthRefresh;
    private bool _photoLoaded;

    [PersistentState]
    public List<BaseClubDto>? UserClubs { get; set; }

    public string? UserPhotoUrl { get; set; }

    private bool IsLoadingPhoto { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        currentUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        navigationManager.LocationChanged += OnLocationChanged;
        userPhotoNotifications.PhotoChanged += OnPhotoChanged;
        userClubStateService.ClubsChanged += OnClubsChanged;

        if (!_authSubscribed)
        {
            authenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
            _authSubscribed = true;
        }

        _authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        await HandleAuthStateAsync(_authState);
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

            var userId = _authState?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (_authState?.User.Identity?.IsAuthenticated is true && userId is not null)
            {
                await StartPhotoNotificationsAsync();
                if (!_photoLoaded)
                {
                    await RefreshUserPhotoAsync();
                }

                if (UserClubs is null || UserClubs.Count == 0)
                {
                    await userClubStateService.EnsureFreshAsync(CancellationToken);
                    UserClubs = userClubStateService.UserClubs?.ToList() ?? UserClubs;
                }

                StateHasChanged();
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

    private void OnPhotoChanged()
    {
        _photoLoaded = false;
        _ = InvokeAsync(RefreshUserPhotoAsync);
    }

    private void OnClubsChanged()
    {
        UserClubs = userClubStateService.UserClubs?.ToList();
        InvokeAsync(StateHasChanged);
    }

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
        _authState = authState;

        // Guard: ensure user is authenticated AND has a valid NameIdentifier claim
        // This can be false during SSR prerender right after registration before claims are hydrated
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (authState.User.Identity?.IsAuthenticated is not true)
        {
            await StopPhotoNotificationsAsync();
            userClubStateService.ClearUserClubs();
            UserPhotoUrl = null;
            UserClubs = null;
            _currentUserId = null;
            _photoLoaded = false;
            IsLoadingPhoto = false;
            return;
        }

        if (userId is null)
        {
            _pendingAuthRefresh = true;
            await StopPhotoNotificationsAsync();
            _photoLoaded = false;
            IsLoadingPhoto = false;
            return;
        }

        _pendingAuthRefresh = false;

        if (_currentUserId is not null && !string.Equals(_currentUserId, userId, StringComparison.Ordinal))
        {
            UserPhotoUrl = null;
            UserClubs = null;
            userClubStateService.ClearUserClubs();
            _photoLoaded = false;
        }

        _currentUserId = userId;

        var shouldFetchPhoto = RendererInfo.IsInteractive;
        if (shouldFetchPhoto)
        {
            await StartPhotoNotificationsAsync();
        }
        else
        {
            await StopPhotoNotificationsAsync();
            UserPhotoUrl = null;
            _photoLoaded = false;
            IsLoadingPhoto = false;
        }

        if (UserClubs is not null && userClubStateService.UserClubs is null)
        {
            userClubStateService.SetUserClubs(UserClubs);
        }

        if (UserClubs is null)
        {
            await userClubStateService.EnsureFreshAsync(CancellationToken);
            UserClubs = userClubStateService.UserClubs?.ToList() ?? [];
        }

        if (shouldFetchPhoto)
        {
            await RefreshUserPhotoAsync();
        }
    }

    private async Task RefreshUserPhotoAsync()
    {
        IsLoadingPhoto = true;

        try
        {
            var result = await calcioUsersService.GetAccountPhotoAsync(CancellationToken);
            if (result.IsSuccess)
            {
                result.Value.Switch(
                    photo => UserPhotoUrl = photo.SmallUrl ?? photo.OriginalUrl,
                    _ => UserPhotoUrl = null);
            }
            else
            {
                UserPhotoUrl = null;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
        {
            UserPhotoUrl = null;
        }
        finally
        {
            _photoLoaded = true;
            IsLoadingPhoto = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task StartPhotoNotificationsAsync()
        => await userPhotoNotifications.StartAsync(CancellationToken);

    private async Task StopPhotoNotificationsAsync()
        => await userPhotoNotifications.StopAsync(CancellationToken);

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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = navigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public override void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
        userPhotoNotifications.PhotoChanged -= OnPhotoChanged;
        userClubStateService.ClubsChanged -= OnClubsChanged;
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
