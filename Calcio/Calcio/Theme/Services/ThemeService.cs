using Microsoft.JSInterop;

namespace Calcio.Theme.Services;

public enum ThemePreference
{
    Light,
    Dark,
    System
}

public sealed class ThemeService(IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private bool _initialized;

    public event Action<ThemePreference>? ThemeChanged;

    public ThemePreference Current { get; private set; } = ThemePreference.System;

    // Must be called only after component is interactive (OnAfterRenderAsync firstRender + RendererInfo.IsInteractive)
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _dotNetRef = DotNetObjectReference.Create(this);
        var prefString = await js.InvokeAsync<string>("calcioTheme.init", _dotNetRef);
        Current = Enum.TryParse(prefString, ignoreCase: true, out ThemePreference parsed)
            ? parsed
            : ThemePreference.System;
        _initialized = true;
        ThemeChanged?.Invoke(Current);
    }

    public async Task SetThemeAsync(ThemePreference preference)
    {
        await InitializeAsync();
        if (Current == preference)
        {
            return;
        }

        Current = preference;
        await js.InvokeVoidAsync("calcioTheme.setPreference", preference.ToString());
        ThemeChanged?.Invoke(Current);
    }

    [JSInvokable]
    public void SystemThemeChanged(string _)
    {
        if (Current == ThemePreference.System)
        {
            ThemeChanged?.Invoke(Current);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            try
            {
                await js.InvokeVoidAsync("calcioTheme.dispose");
            }
            catch { }

            _dotNetRef.Dispose();
        }
    }
}
