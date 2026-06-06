using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components;

/// <summary>
/// Provides a base Blazor component that exposes a shared cancellation token and cancels it when disposed.
/// </summary>
public abstract class CancellableComponentBase : ComponentBase, IDisposable
{
    /// <summary>
    /// Stores the cancellation token source used by asynchronous component operations.
    /// </summary>
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Gets a cancellation token that is canceled when the component is disposed.
    /// </summary>
    protected CancellationToken CancellationToken => (_cts ??= new()).Token;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    /// <summary>
    /// Cancels and disposes the current cancellation token source.
    /// </summary>
    public virtual void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
