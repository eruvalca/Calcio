using Microsoft.JSInterop;

namespace Calcio.UI.Services.Theme;

/// <summary>
/// Represents available UI theme preferences.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Uses the light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Uses the dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Uses the operating system theme preference.
    /// </summary>
    System
}

/// <summary>
/// Coordinates theme initialization and updates through the browser JavaScript module.
/// </summary>
/// <param name="js">The JavaScript runtime used to invoke theme functions.</param>
public sealed class ThemeService(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>
    /// Lazily loads the JavaScript module that applies theme changes.
    /// </summary>
    private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Calcio.UI/theme.js").AsTask());

    /// <summary>
    /// Holds the .NET reference passed to JavaScript callbacks.
    /// </summary>
    private DotNetObjectReference<ThemeService>? _dotNetRef;

    /// <summary>
    /// Indicates whether JavaScript initialization has completed.
    /// </summary>
    private bool _initialized;

    /// <summary>
    /// Raised when the effective theme preference changes.
    /// </summary>
    public event Action<ThemePreference>? ThemeChanged;

    /// <summary>
    /// Gets the current theme preference.
    /// </summary>
    public ThemePreference Current { get; private set; } = ThemePreference.System;

    /// <summary>
    /// Initializes theme interop and synchronizes the current preference from JavaScript.
    /// </summary>
    /// <returns>A task that completes after initialization finishes.</returns>
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

    /// <summary>
    /// Applies a new theme preference and persists it through JavaScript.
    /// </summary>
    /// <param name="preference">The preferred theme to apply.</param>
    /// <returns>A task that completes after the preference is applied.</returns>
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
    /// <summary>
    /// Handles system theme change notifications from JavaScript.
    /// </summary>
    public void SystemThemeChanged()
    {
        if (Current == ThemePreference.System)
        {
            ThemeChanged?.Invoke(Current);
        }
    }

    /// <summary>
    /// Releases JavaScript resources associated with theme handling.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
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
