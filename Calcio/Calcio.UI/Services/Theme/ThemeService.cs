using Microsoft.JSInterop;

namespace Calcio.UI.Services.Theme;

public enum ThemePreference
{
    Light,
    Dark,
    System
}

public sealed class ThemeService(IJSRuntime js) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Calcio.UI/theme.js").AsTask());

    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private bool _initialized;

    public event Action<ThemePreference>? ThemeChanged;

    public ThemePreference Current { get; private set; } = ThemePreference.System;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _dotNetRef = DotNetObjectReference.Create(this);
        var module = await moduleTask.Value;
        var prefString = await module.InvokeAsync<string>("init", _dotNetRef);
        Current = Enum.TryParse(prefString, true, out ThemePreference parsed) ? parsed : ThemePreference.System;
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
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("setPreference", preference.ToString());
        ThemeChanged?.Invoke(Current);
    }

    [JSInvokable]
    public void SystemThemeChanged()
    {
        if (Current == ThemePreference.System)
        {
            ThemeChanged?.Invoke(Current);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            try
            {
                if (moduleTask.IsValueCreated)
                {
                    var module = await moduleTask.Value;
                    await module.InvokeVoidAsync("dispose");
                    await module.DisposeAsync();
                }
            }
            catch
            {
                // Swallow JS disposal exceptions.
            }

            _dotNetRef.Dispose();
        }
    }
}
