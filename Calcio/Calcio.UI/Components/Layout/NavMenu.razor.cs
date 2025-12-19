using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Calcio.UI.Components.Layout;

public partial class NavMenu(
    NavigationManager navigationManager,
    IClubsService clubsService,
    ThemeService themeService)
{
    private string? currentUrl;
    private bool _themeSubscribed;

    private List<BaseClubDto> UserClubs { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        currentUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        navigationManager.LocationChanged += OnLocationChanged;

        var userClubsResult = await clubsService.GetUserClubsAsync(CancellationToken);
        userClubsResult.Switch(
            clubs => UserClubs = clubs,
            problem => UserClubs = []);
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
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        navigationManager.LocationChanged -= OnLocationChanged;
        if (_themeSubscribed)
        {
            themeService.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose();
    }
}
