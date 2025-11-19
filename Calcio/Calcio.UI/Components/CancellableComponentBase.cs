using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components;

public abstract class CancellableComponentBase : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cts;

    protected CancellationToken CancellationToken => (_cts ??= new()).Token;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public virtual void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}

