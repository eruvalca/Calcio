using System.Reflection;
using System.Threading;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Services.CalcioUsers;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using OneOf;
using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Services.CalcioUsers;

public sealed class UserPhotoStateServiceTests
{
    private static T? GetField<T>(UserPhotoStateService service, string name)
        => (T?)typeof(UserPhotoStateService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service);

    private static void SetField(UserPhotoStateService service, string name, object? value)
        => typeof(UserPhotoStateService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, value);

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenCleared_ShouldAttemptRefresh()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        OneOf<CalcioUserPhotoDto, None> noneResult = new None();
        calcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(noneResult));

        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);

        // Act
        service.ClearPhoto();
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await calcioUsersService.Received(1).GetAccountPhotoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenFailure_ShouldThrottleRetry()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        calcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(ServiceProblem.ServerError()));

        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);

        // Act
        await service.EnsureFreshAsync(CancellationToken.None);
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await calcioUsersService.Received(1).GetAccountPhotoAsync(Arg.Any<CancellationToken>());

        // Act - advance past retry interval
        timeProvider.Advance(UserPhotoStateService.RetryInterval + TimeSpan.FromSeconds(1));
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await calcioUsersService.Received(2).GetAccountPhotoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenRecentSuccess_ShouldNotRefreshUntilStale()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", "https://example.com/small.jpg", null, null);
        OneOf<CalcioUserPhotoDto, None> photoResult = photo;

        calcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(photoResult));

        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);

        // Act
        await service.EnsureFreshAsync(CancellationToken.None);
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await calcioUsersService.Received(1).GetAccountPhotoAsync(Arg.Any<CancellationToken>());

        // Act - advance past refresh interval
        timeProvider.Advance(UserPhotoStateService.RefreshInterval + TimeSpan.FromSeconds(1));
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await calcioUsersService.Received(2).GetAccountPhotoAsync(Arg.Any<CancellationToken>());
        service.PhotoUrl.ShouldBe("https://example.com/small.jpg");
    }

    [Fact]
    public void UpdateFromPhoto_ShouldSetPhotoUrlAndRaiseEvent()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);
        var photo = new CalcioUserPhotoDto(1, "https://example.com/original.jpg", "https://example.com/small.jpg", null, null);
        var eventFired = false;
        service.PhotoChanged += () => eventFired = true;

        // Act
        service.UpdateFromPhoto(photo);

        // Assert
        service.PhotoUrl.ShouldBe("https://example.com/small.jpg");
        eventFired.ShouldBeTrue();
    }

    [Fact]
    public void ClearPhoto_ShouldResetStateAndRaiseEvent()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);
        var eventFired = false;
        service.PhotoChanged += () => eventFired = true;
        service.SetPhotoUrl("https://example.com/photo.jpg");

        // Act
        service.ClearPhoto();

        // Assert
        service.PhotoUrl.ShouldBeNull();
        eventFired.ShouldBeTrue();
    }

    [Fact]
    public void StartAndStopAutoRefresh_ShouldManageTimerState()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);

        // Act
        service.StartAutoRefresh();

        // Assert
        GetField<bool>(service, "_autoRefreshStarted").ShouldBeTrue();
        GetField<PeriodicTimer?>(service, "_refreshTimer").ShouldNotBeNull();
        GetField<CancellationTokenSource?>(service, "_refreshCts").ShouldNotBeNull();
        GetField<Task?>(service, "_refreshLoop").ShouldNotBeNull();

        // Act
        service.StopAutoRefresh();

        // Assert
        GetField<bool>(service, "_autoRefreshStarted").ShouldBeFalse();
        GetField<PeriodicTimer?>(service, "_refreshTimer").ShouldBeNull();
        GetField<CancellationTokenSource?>(service, "_refreshCts").ShouldBeNull();
    }

    [Fact]
    public async Task RunRefreshLoopAsync_ShouldInvokeRefresh()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var calcioUsersService = Substitute.For<ICalcioUsersService>();
        OneOf<CalcioUserPhotoDto, None> noneResult = new None();
        var callCount = 0;
        calcioUsersService.GetAccountPhotoAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult<ServiceResult<OneOf<CalcioUserPhotoDto, None>>>(noneResult);
            });

        var service = new UserPhotoStateService(calcioUsersService, timeProvider, NullLogger<UserPhotoStateService>.Instance);
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(5));
        SetField(service, "_refreshTimer", timer);
        SetField(service, "_lastUpdated", null);
        SetField(service, "_lastAttempt", null);

        var runMethod = typeof(UserPhotoStateService).GetMethod("RunRefreshLoopAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        runMethod.ShouldNotBeNull();

        using var cts = new CancellationTokenSource();
        var loopTask = (Task)runMethod.Invoke(service, [cts.Token])!;

        try
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(500);
            while (DateTime.UtcNow < deadline && callCount == 0)
            {
                await Task.Delay(10);
            }
        }
        finally
        {
            cts.Cancel();
            await loopTask;
            timer.Dispose();
            SetField(service, "_refreshTimer", null);
        }

        // Assert
        callCount.ShouldBeGreaterThan(0);
    }
}
