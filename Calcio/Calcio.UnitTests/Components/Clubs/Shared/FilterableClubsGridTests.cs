using Bunit;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.UI.Components.Clubs.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.Clubs.Shared;

/// <summary>
/// Unit tests for the FilterableClubsGrid Blazor component using bUnit.
/// 
/// This component displays a searchable grid of clubs with:
/// - Search/filter functionality by name, city, or state
/// - "Request to Join" button for clubs without a pending request
/// - "Pending" badge and "Cancel Request" button for the club with a pending request
/// - Confirmation modals for both join and cancel actions
/// </summary>
public sealed class FilterableClubsGridTests : BunitContext
{
    private readonly IClubJoinRequestService _mockClubJoinRequestService;
    private readonly NavigationManager _mockNavigationManager;

    public FilterableClubsGridTests()
    {
        _mockClubJoinRequestService = Substitute.For<IClubJoinRequestService>();

        Services.AddSingleton(_mockClubJoinRequestService);

        // bUnit provides a fake NavigationManager that we can use
        // It's automatically registered, but we capture it for verification
        _mockNavigationManager = Services.GetRequiredService<NavigationManager>();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    private static List<BaseClubDto> CreateTestClubs(int count = 3)
        => [
            .. Enumerable.Range(1, count)
                .Select(i => new BaseClubDto(
                    Id: i,
                    Name: $"Test Club {i}",
                    City: $"City {i}",
                    State: $"State {i}"))
        ];

    private IRenderedComponent<FilterableClubsGrid> RenderGrid(
        List<BaseClubDto>? clubs = null,
        ClubJoinRequestDto? currentJoinRequest = null)
            => Render<FilterableClubsGrid>(parameters => parameters
                .Add(p => p.Clubs, clubs ?? [])
                .Add(p => p.CurrentJoinRequest, currentJoinRequest));

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenNoClubs_ShouldRenderEmptyGrid()
    {
        // Arrange & Act
        var cut = RenderGrid(clubs: []);

        // Assert
        cut.Find("table").ShouldNotBeNull();
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }

    [Fact]
    public void WhenClubsExist_ShouldDisplayAllClubs()
    {
        // Arrange
        var clubs = CreateTestClubs(3);

        // Act
        var cut = RenderGrid(clubs: clubs);

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);

        cut.Markup.ShouldContain("Test Club 1");
        cut.Markup.ShouldContain("City 2");
        cut.Markup.ShouldContain("State 3");
    }

    [Fact]
    public void WhenNoPendingRequest_ShouldShowJoinButtonForAllClubs()
    {
        // Arrange
        var clubs = CreateTestClubs(2);

        // Act
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: null);

        // Assert
        var joinButtons = cut.FindAll("button.btn-outline-primary");
        joinButtons.Count.ShouldBe(2);

        foreach (var button in joinButtons)
        {
            button.TextContent.Trim().ShouldBe("Request to Join");
        }

        cut.FindAll(".badge.bg-warning").ShouldBeEmpty();
    }

    [Fact]
    public void WhenHasPendingRequest_ShouldShowPendingBadgeAndCancelButton()
    {
        // Arrange
        var clubs = CreateTestClubs(3);
        var pendingRequest = new ClubJoinRequestDto(1, 2, 100, RequestStatus.Pending);

        // Act
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        // Assert
        // The pending club should have a badge and cancel button
        var pendingBadges = cut.FindAll(".badge.bg-warning");
        pendingBadges.Count.ShouldBe(1);
        pendingBadges[0].TextContent.ShouldContain("Pending");

        var cancelButtons = cut.FindAll("button.btn-outline-danger");
        cancelButtons.Count.ShouldBe(1);
        cancelButtons[0].TextContent.Trim().ShouldBe("Cancel Request");

        // Other clubs should not have join buttons (since user already has pending request)
        cut.FindAll("button.btn-outline-primary").ShouldBeEmpty();
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public void WhenSearchByName_ShouldFilterClubs()
    {
        // Arrange
        var clubs = new List<BaseClubDto>
        {
            new(1, "Alpha FC", "New York", "NY"),
            new(2, "Beta United", "Los Angeles", "CA"),
            new(3, "Gamma FC", "Chicago", "IL")
        };
        var cut = RenderGrid(clubs: clubs);

        // Act
        var searchInput = cut.Find("input#ClubSearch");
        searchInput.Input("Alpha");

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        cut.Markup.ShouldContain("Alpha FC");
        cut.Markup.ShouldNotContain("Beta United");
    }

    [Fact]
    public void WhenSearchByCity_ShouldFilterClubs()
    {
        // Arrange
        var clubs = new List<BaseClubDto>
        {
            new(1, "Club A", "New York", "NY"),
            new(2, "Club B", "Los Angeles", "CA"),
            new(3, "Club C", "New Orleans", "LA")
        };
        var cut = RenderGrid(clubs: clubs);

        // Act
        cut.Find("input#ClubSearch").Input("New");

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2); // New York and New Orleans
        cut.Markup.ShouldContain("Club A");
        cut.Markup.ShouldContain("Club C");
        cut.Markup.ShouldNotContain("Club B");
    }

    [Fact]
    public void WhenSearchByState_ShouldFilterClubs()
    {
        // Arrange
        var clubs = new List<BaseClubDto>
        {
            new(1, "Club A", "Austin", "TX"),
            new(2, "Club B", "Houston", "TX"),
            new(3, "Club C", "Miami", "FL")
        };
        var cut = RenderGrid(clubs: clubs);

        // Act
        cut.Find("input#ClubSearch").Input("TX");

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);
        cut.Markup.ShouldContain("Club A");
        cut.Markup.ShouldContain("Club B");
        cut.Markup.ShouldNotContain("Club C");
    }

    [Fact]
    public void WhenSearchIsCaseInsensitive_ShouldFilterCorrectly()
    {
        // Arrange
        var clubs = new List<BaseClubDto>
        {
            new(1, "ALPHA FC", "NEW YORK", "NY"),
            new(2, "beta united", "los angeles", "ca")
        };
        var cut = RenderGrid(clubs: clubs);

        // Act
        cut.Find("input#ClubSearch").Input("alpha");

        // Assert
        cut.FindAll("tbody tr").Count.ShouldBe(1);
        cut.Markup.ShouldContain("ALPHA FC");
    }

    [Fact]
    public void WhenSearchCleared_ShouldShowAllClubs()
    {
        // Arrange
        var clubs = CreateTestClubs(3);
        var cut = RenderGrid(clubs: clubs);

        // Filter first
        cut.Find("input#ClubSearch").Input("Test Club 1");
        cut.FindAll("tbody tr").Count.ShouldBe(1);

        // Act - Clear search
        cut.Find("input#ClubSearch").Input("");

        // Assert
        cut.FindAll("tbody tr").Count.ShouldBe(3);
    }

    [Fact]
    public void WhenNoMatchingResults_ShouldShowEmptyGrid()
    {
        // Arrange
        var clubs = CreateTestClubs(3);
        var cut = RenderGrid(clubs: clubs);

        // Act
        cut.Find("input#ClubSearch").Input("NonExistentClub");

        // Assert
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }

    #endregion

    #region Join Request Modal Tests

    [Fact]
    public void WhenJoinClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        // Act
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        cut.Markup.ShouldContain("Confirm Join Request");
        cut.Markup.ShouldContain("Test Club 1");
        cut.Markup.ShouldContain("club administrator will need to approve");
    }

    [Fact]
    public void WhenJoinCancelClicked_ShouldHideModal()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);
        cut.Find("button.btn-outline-primary").Click();

        // Act
        cut.Find(".modal-footer button.btn-secondary").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    [Fact]
    public void WhenJoinModalCloseButtonClicked_ShouldHideModal()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);
        cut.Find("button.btn-outline-primary").Click();

        // Act
        cut.Find(".modal-header .btn-close").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenJoinConfirmed_ShouldCallService()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Success());

        cut.Find("button.btn-outline-primary").Click();

        // Act
        await cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenJoinReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        cut.Find("button.btn-outline-primary").Click();

        // Act
        await cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("could not be found");
        });
    }

    [Fact]
    public async Task WhenJoinReturnsConflict_ShouldDisplayError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Conflict());

        cut.Find("button.btn-outline-primary").Click();

        // Act
        await cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("already have a pending request");
        });
    }

    [Fact]
    public async Task WhenJoinReturnsUnauthorized_ShouldDisplayError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        cut.Find("button.btn-outline-primary").Click();

        // Act
        await cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("must be logged in");
        });
    }

    [Fact]
    public async Task WhenJoinReturnsError_ShouldDisplayGenericError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Error());

        cut.Find("button.btn-outline-primary").Click();

        // Act
        await cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Cancel Request Modal Tests

    [Fact]
    public void WhenCancelRequestClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        // Act
        cut.Find("button.btn-outline-danger").Click();

        // Assert
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        cut.Markup.ShouldContain("Cancel Join Request");
        cut.Markup.ShouldContain("Test Club 1");
    }

    [Fact]
    public void WhenCancelRequestDismissed_ShouldHideModal()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);
        cut.Find("button.btn-outline-danger").Click();

        // Act - Click "Keep Request" button
        cut.Find(".modal-footer button.btn-secondary").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenCancelRequestConfirmed_ShouldCallService()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        _mockClubJoinRequestService
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>())
            .Returns(new Success());

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCancelRequestReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        _mockClubJoinRequestService
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("No pending request found");
        });
    }

    [Fact]
    public async Task WhenCancelRequestReturnsUnauthorized_ShouldDisplayError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        _mockClubJoinRequestService
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("must be logged in");
        });
    }

    [Fact]
    public async Task WhenCancelRequestReturnsError_ShouldDisplayGenericError()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        _mockClubJoinRequestService
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>())
            .Returns(new Error());

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Button State Tests

    [Fact]
    public async Task WhenJoinProcessing_ShouldDisableButtonsAndShowSpinner()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var cut = RenderGrid(clubs: clubs);

        var tcs = new TaskCompletionSource<OneOf.OneOf<Success, NotFound, Conflict, Unauthorized, Error>>();
        _mockClubJoinRequestService
            .CreateJoinRequestAsync(1, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-outline-primary").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-primary").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.Find(".modal-footer button.btn-primary");
            confirmButton.HasAttribute("disabled").ShouldBeTrue();

            var cancelButton = cut.Find(".modal-footer button.btn-secondary");
            cancelButton.HasAttribute("disabled").ShouldBeTrue();

            cut.FindAll(".spinner-border").Count.ShouldBe(1);
        });

        // Cleanup
        tcs.SetResult(new Success());
        await clickTask;
    }

    [Fact]
    public async Task WhenCancelProcessing_ShouldDisableButtonsAndShowSpinner()
    {
        // Arrange
        var clubs = CreateTestClubs(1);
        var pendingRequest = new ClubJoinRequestDto(1, 1, 100, RequestStatus.Pending);
        var cut = RenderGrid(clubs: clubs, currentJoinRequest: pendingRequest);

        var tcs = new TaskCompletionSource<OneOf.OneOf<Success, NotFound, Unauthorized, Error>>();
        _mockClubJoinRequestService
            .CancelJoinRequestAsync(Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.Find(".modal-footer button.btn-danger");
            confirmButton.HasAttribute("disabled").ShouldBeTrue();

            cut.FindAll(".spinner-border").Count.ShouldBe(1);
        });

        // Cleanup
        tcs.SetResult(new Success());
        await clickTask;
    }

    #endregion

    #region Multiple Clubs Tests

    [Fact]
    public void WhenMultipleClubs_ShouldJoinCorrectOne()
    {
        // Arrange
        var clubs = CreateTestClubs(3);
        var cut = RenderGrid(clubs: clubs);

        // Act - Click join on the second club
        var joinButtons = cut.FindAll("button.btn-outline-primary");
        joinButtons[1].Click();

        // Assert - Modal should show the correct club name in the modal body
        // The modal body contains "Are you sure you want to request to join <strong>Test Club 2</strong>?"
        var modalBody = cut.Find(".modal-body");
        modalBody.InnerHtml.ShouldContain("<strong>Test Club 2</strong>");
    }

    #endregion
}
