using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.Extensions.Logging;

namespace Calcio.UI.Services.CalcioUsers;

public sealed partial class UserPhotoStateService(
    ICalcioUsersService calcioUsersService,
    TimeProvider timeProvider,
    ILogger<UserPhotoStateService> logger) : IAsyncDisposable
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(55);
    public static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource? _refreshCts;
    private Task? _refreshLoop;
    private DateTimeOffset? _lastUpdated;
    private DateTimeOffset? _lastAttempt;
    private bool _autoRefreshStarted;

    public event Action? PhotoChanged;

    public string? PhotoUrl { get; private set; }

    public void SetPhotoUrl(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        var now = timeProvider.GetUtcNow();
        _lastUpdated = now;
        _lastAttempt = now;
        PhotoChanged?.Invoke();
    }

    public void ClearPhoto()
    {
        PhotoUrl = null;
        _lastUpdated = null;
        _lastAttempt = null;
        PhotoChanged?.Invoke();
    }

    public void UpdateFromPhoto(CalcioUserPhotoDto? photo)
        => SetPhotoUrl(photo?.SmallUrl ?? photo?.OriginalUrl);

    public void StartAutoRefresh()
    {
        if (_autoRefreshStarted)
        {
            return;
        }

        _autoRefreshStarted = true;
        _refreshCts = new CancellationTokenSource();
        _refreshTimer = new PeriodicTimer(RetryInterval);
        _refreshLoop = RunRefreshLoopAsync(_refreshCts.Token);
    }

    public void StopAutoRefresh()
    {
        if (!_autoRefreshStarted)
        {
            return;
        }

        _autoRefreshStarted = false;
        _refreshCts?.Cancel();
        _refreshTimer?.Dispose();
        _refreshTimer = null;

        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    public async Task EnsureFreshAsync(CancellationToken cancellationToken)
    {
        if (ShouldRefresh())
        {
            await RefreshAsync(cancellationToken);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!ShouldRefresh())
            {
                return;
            }

            var now = timeProvider.GetUtcNow();
            _lastAttempt = now;

            try
            {
                var result = await calcioUsersService.GetAccountPhotoAsync(cancellationToken);
                if (result.IsSuccess)
                {
                    UpdateFromPhoto(result.Value.Match<CalcioUserPhotoDto?>(
                        photo => photo,
                        _ => null));
                    return;
                }
            }
            catch (Exception ex)
            {
                LogRefreshFailed(logger, ex);
                return;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopAutoRefresh();

        var refreshLoop = _refreshLoop;
        _refreshLoop = null;
        if (refreshLoop is not null)
        {
            try
            {
                await refreshLoop;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private bool ShouldRefresh()
    {
        var now = timeProvider.GetUtcNow();

        if (_lastAttempt is not null && now - _lastAttempt < RetryInterval)
        {
            return false;
        }

        if (_lastUpdated is null)
        {
            return true;
        }

        return now - _lastUpdated >= RefreshInterval;
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        if (_refreshTimer is null)
        {
            return;
        }

        try
        {
            while (await _refreshTimer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await RefreshAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    LogRefreshLoopFailed(logger, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Failed to refresh user photo.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Warning, "User photo refresh loop failed.")]
    private static partial void LogRefreshLoopFailed(ILogger logger, Exception exception);
}
