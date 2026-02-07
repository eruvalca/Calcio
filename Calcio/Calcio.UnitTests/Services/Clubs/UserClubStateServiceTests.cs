using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Services.Clubs;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Services.Clubs;

public sealed class UserClubStateServiceTests
{
    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenCleared_ShouldFetchAndSetClubs()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var clubsService = Substitute.For<IClubsService>();
        var clubs = new List<BaseClubDto>
        {
            new(10, "Club A", "City", "ST")
        };

        clubsService.GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(clubs)));

        var service = new UserClubStateService(clubsService, timeProvider, NullLogger<UserClubStateService>.Instance);
        service.ClearUserClubs();

        // Act
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await clubsService.Received(1).GetUserClubsAsync(Arg.Any<CancellationToken>());
        service.UserClubs.ShouldNotBeNull();
        service.UserClubs!.Count.ShouldBe(1);
        service.UserClubs[0].Id.ShouldBe(10);
    }

    [Fact]
    public async Task EnsureFreshAsync_WhenFailure_ShouldThrottleRetry()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var clubsService = Substitute.For<IClubsService>();
        clubsService.GetUserClubsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<List<BaseClubDto>>(ServiceProblem.ServerError())));

        var service = new UserClubStateService(clubsService, timeProvider, NullLogger<UserClubStateService>.Instance);

        // Act
        await service.EnsureFreshAsync(CancellationToken.None);
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await clubsService.Received(1).GetUserClubsAsync(Arg.Any<CancellationToken>());

        // Act - advance past retry interval
        timeProvider.Advance(UserClubStateService.RetryInterval + TimeSpan.FromSeconds(1));
        await service.EnsureFreshAsync(CancellationToken.None);

        // Assert
        await clubsService.Received(2).GetUserClubsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SetUserClubs_ShouldSetListAndRaiseEvent()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var clubsService = Substitute.For<IClubsService>();
        var service = new UserClubStateService(clubsService, timeProvider, NullLogger<UserClubStateService>.Instance);
        var eventFired = false;
        service.ClubsChanged += () => eventFired = true;

        // Act
        service.SetUserClubs([new BaseClubDto(2, "Club B", "Town", "TS")]);

        // Assert
        service.UserClubs.ShouldNotBeNull();
        service.UserClubs!.Count.ShouldBe(1);
        service.UserClubs[0].Name.ShouldBe("Club B");
        eventFired.ShouldBeTrue();
    }
}
