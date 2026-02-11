using Calcio.UI.Services.CalcioUsers;

using Microsoft.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Services.CalcioUsers;

public sealed class UserPhotoNotificationsServiceTests
{
    private readonly NavigationManager _navigationManager;
    private readonly UserPhotoNotificationsService _sut;

    public UserPhotoNotificationsServiceTests()
    {
        _navigationManager = Substitute.For<NavigationManager>();
        _sut = new UserPhotoNotificationsService(_navigationManager);
    }

    [Fact]
    public async Task StartAsync_WhenNotOnBrowser_ShouldNotThrow()
    {
        // On non-browser (test host), StartAsync should be a no-op
        var ex = await Record.ExceptionAsync(() => _sut.StartAsync(CancellationToken.None).AsTask());
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _sut.StopAsync(CancellationToken.None).AsTask());
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotStarted_ShouldNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _sut.DisposeAsync().AsTask());
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        await _sut.DisposeAsync();
        var ex = await Record.ExceptionAsync(() => _sut.DisposeAsync().AsTask());
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ShouldNotThrow()
    {
        await _sut.DisposeAsync();
        var ex = await Record.ExceptionAsync(() => _sut.StopAsync(CancellationToken.None).AsTask());
        ex.ShouldBeNull();
    }

    [Fact]
    public void PhotoChanged_Event_ShouldBeSubscribable()
    {
        var fired = false;
        _sut.PhotoChanged += () => fired = true;

        // Event is exposed but only fired by SignalR — verify subscription doesn't throw
        fired.ShouldBeFalse();
    }
}
