using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Services.Clubs;

using Microsoft.Extensions.Logging;

namespace Calcio.UI.Services.Clubs;

public sealed partial class UserClubStateService(
    IClubsService clubsService,
    TimeProvider timeProvider,
    ILogger<UserClubStateService> logger)
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(55);
    public static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _lastUpdated;
    private DateTimeOffset? _lastAttempt;

    public event Action? ClubsChanged;

    public IReadOnlyList<BaseClubDto>? UserClubs { get; private set; }

    public void SetUserClubs(IEnumerable<BaseClubDto> clubs)
    {
        UserClubs = [.. clubs];
        var now = timeProvider.GetUtcNow();
        _lastUpdated = now;
        _lastAttempt = now;
        ClubsChanged?.Invoke();
    }

    public void ClearUserClubs()
    {
        UserClubs = null;
        _lastUpdated = null;
        _lastAttempt = null;
        ClubsChanged?.Invoke();
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

            _lastAttempt = timeProvider.GetUtcNow();

            try
            {
                var result = await clubsService.GetUserClubsAsync(cancellationToken);
                if (result.IsSuccess)
                {
                    SetUserClubs(result.Value);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogRefreshFailed(logger, ex);
            }
        }
        finally
        {
            _refreshLock.Release();
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

    [LoggerMessage(1, LogLevel.Warning, "Failed to refresh user clubs.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception);
}
