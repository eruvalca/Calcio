using System.Security.Claims;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Calcio.UI.Components.Layout;

public partial class NavMenu(
    NavigationManager navigationManager,
    IClubsService clubsService,
    ICalcioUsersService calcioUsersService,
    ThemeService themeService,
    AuthenticationStateProvider authenticationStateProvider)
{
    private string? currentUrl;
    private bool _themeSubscribed;

    [PersistentState]
    public List<BaseClubDto>? UserClubs { get; set; }

    [PersistentState]
    public string? UserPhotoUrl { get; set; }

    private bool IsLoadingPhoto { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        currentUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        navigationManager.LocationChanged += OnLocationChanged;

        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();

        // Guard: ensure user is authenticated AND has a valid NameIdentifier claim
        // This can be false during SSR prerender right after registration before claims are hydrated
        var hasValidUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value is not null;
        if (authState.User.Identity?.IsAuthenticated is not true || !hasValidUserId)
        {
            IsLoadingPhoto = false;
            return;
        }

        // If state was restored from prerender, skip fetching
        if (UserClubs is not null)
        {
            IsLoadingPhoto = false;
            return;
        }

        // No persisted state (SSR prerender) - fetch from services
        var userClubsResult = await clubsService.GetUserClubsAsync(CancellationToken);
        userClubsResult.Switch(
            fetchedClubs => UserClubs = fetchedClubs,
            problem => UserClubs = []);

        // Wrap in try-catch because HttpContext.User may differ from Blazor's AuthenticationState
        // during SSR prerender immediately after authentication changes
        try
        {
            var photoResult = await calcioUsersService.GetAccountPhotoAsync(CancellationToken);
            photoResult.Switch(
                photoOrNone => photoOrNone.Switch(
                    photo => UserPhotoUrl = photo.SmallUrl,
                    _ => UserPhotoUrl = null),
                _ => UserPhotoUrl = null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
        {
            // User claims not yet available in HttpContext - fall back to no photo
            UserPhotoUrl = null;
        }

        IsLoadingPhoto = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive && !_themeSubscribed)
        {
            await themeService.InitializeAsync();
            themeService.ThemeChanged += OnThemeChanged;
            _themeSubscribed = true;
            StateHasChanged();
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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = navigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public override void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
        if (_themeSubscribed)
        {
            themeService.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose();
    }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
}
