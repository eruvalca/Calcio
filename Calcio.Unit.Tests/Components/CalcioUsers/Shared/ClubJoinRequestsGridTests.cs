using Bunit;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.UI.Components.CalcioUsers.Shared;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.CalcioUsers.Shared;

/// <summary>
/// Unit tests for the ClubJoinRequestsGrid Blazor component using bUnit.
/// </summary>
public sealed class ClubJoinRequestsGridTests : BunitContext
{
    private readonly IClubJoinRequestsService _mockClubJoinRequestService;

    public ClubJoinRequestsGridTests()
    {
        _mockClubJoinRequestService = Substitute.For<IClubJoinRequestsService>();
        Services.AddSingleton(_mockClubJoinRequestService);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    /// <summary>
    /// Creates test data for join requests.
    /// </summary>
    private static List<ClubJoinRequestWithUserDto> CreateTestJoinRequests(int count = 2)
        => [
            .. Enumerable.Range(1, count)
                .Select(i => new ClubJoinRequestWithUserDto(
                    ClubJoinRequestId: i,
                    ClubId: 100,
                    RequestingUserId: 1000 + i,
                    RequestingUserFullName: $"Test User {i}",
                    RequestingUserEmail: $"user{i}@test.com",
                    Status: RequestStatus.Pending,
                    RequestedAt: DateTimeOffset.Now.AddDays(-i)))
        ];

    /// <summary>
    /// Renders the component with specified parameters.
    /// </summary>
    private IRenderedComponent<ClubJoinRequestsGrid> RenderGrid(
        long clubId = 100,
        List<ClubJoinRequestWithUserDto>? joinRequests = null)
            => Render<ClubJoinRequestsGrid>(parameters => parameters
                .Add(p => p.ClubId, clubId)
                .Add(p => p.JoinRequests, joinRequests ?? []));

    #endregion

    #region Initial Rendering Tests

    /// <summary>
    /// Verifies the component renders the empty state correctly.
    /// </summary>
    [Fact]
    public void WhenNoJoinRequests_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid(joinRequests: []);

        // Assert
        var emptyMessage = cut.Find(".text-muted");
        emptyMessage.TextContent.ShouldBe("No pending join requests.");
        cut.FindAll("table").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the component renders join requests in a grid.
    /// </summary>
    [Fact]
    public void WhenJoinRequestsExist_ShouldDisplayGrid()
    {
        // Arrange
        var requests = CreateTestJoinRequests(2);

        // Act
        var cut = RenderGrid(joinRequests: requests);

        // Assert
        cut.Find("table").ShouldNotBeNull();
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);
        cut.Markup.ShouldContain("Test User 1");
        cut.Markup.ShouldContain("user1@test.com");
    }

    /// <summary>
    /// Verifies each request has approve and reject buttons.
    /// </summary>
    [Fact]
    public void WhenJoinRequestsExist_ShouldDisplayActionButtons()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);

        // Act
        var cut = RenderGrid(joinRequests: requests);

        // Assert
        var approveButtons = cut.FindAll("button.btn-outline-success");
        var rejectButtons = cut.FindAll("button.btn-outline-danger");

        approveButtons.Count.ShouldBe(1);
        rejectButtons.Count.ShouldBe(1);

        approveButtons[0].TextContent.Trim().ShouldBe("Approve");
        rejectButtons[0].TextContent.Trim().ShouldBe("Reject");
    }

    #endregion

    #region Approve Modal Tests

    /// <summary>
    /// Verifies that clicking approve shows the confirmation modal.
    /// </summary>
    [Fact]
    public void WhenApproveClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);

        // Act
        cut.Find("button.btn-outline-success").Click();

        // Assert
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        cut.Markup.ShouldContain("Approve Join Request");
        cut.Markup.ShouldContain("Test User 1");
    }

    /// <summary>
    /// Verifies that clicking cancel closes the approve modal.
    /// </summary>
    [Fact]
    public void WhenApproveCancelClicked_ShouldHideModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);
        cut.Find("button.btn-outline-success").Click();

        // Act
        // Find the cancel button in the modal footer
        cut.Find(".modal-footer button.btn-secondary").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that clicking the X button closes the approve modal.
    /// </summary>
    [Fact]
    public void WhenApproveModalCloseButtonClicked_ShouldHideModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);
        cut.Find("button.btn-outline-success").Click();

        // Act
        cut.Find(".modal-header .btn-close").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that confirming approval calls the service with the correct parameters.
    /// </summary>
    [Fact]
    public async Task WhenApproveConfirmed_ShouldCallService()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        cut.Find("button.btn-outline-success").Click();

        // Act
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an error message is displayed when approval fails with NotFound.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.NotFound()));

        cut.Find("button.btn-outline-success").Click();

        // Act
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("could not be found");
        });
    }

    /// <summary>
    /// Verifies that an error message is displayed when approval fails with Forbidden.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsForbidden_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Forbidden()));

        cut.Find("button.btn-outline-success").Click();

        // Act
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    /// <summary>
    /// Verifies that a generic error message is displayed when approval fails with a server error.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsServerError_ShouldDisplayGenericError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.ServerError()));

        cut.Find("button.btn-outline-success").Click();

        // Act
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Reject Modal Tests

    /// <summary>
    /// Verifies that clicking reject shows the confirmation modal.
    /// </summary>
    [Fact]
    public void WhenRejectClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);

        // Act
        cut.Find("button.btn-outline-danger").Click();

        // Assert
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        cut.Markup.ShouldContain("Reject Join Request");
        cut.Markup.ShouldContain("Test User 1");
    }

    /// <summary>
    /// Verifies that clicking cancel closes the reject modal.
    /// </summary>
    [Fact]
    public void WhenRejectCancelClicked_ShouldHideModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);
        cut.Find("button.btn-outline-danger").Click();

        // Act
        cut.Find(".modal-footer button.btn-secondary").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that confirming rejection calls the service with the correct parameters.
    /// </summary>
    [Fact]
    public async Task WhenRejectConfirmed_ShouldCallService()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Rejected, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Rejected, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an error message is displayed when rejection fails with NotFound.
    /// </summary>
    [Fact]
    public async Task WhenRejectReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Rejected, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.NotFound()));

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("could not be found");
        });
    }

    #endregion

    #region Button State Tests

    /// <summary>
    /// Verifies that buttons are disabled while an action is being processed.
    /// </summary>
    [Fact]
    public async Task WhenProcessing_ShouldDisableButtons()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        // Setup mock to delay response so we can check the processing state
        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-outline-success").Click();

        // Act - Start the async operation
        var clickTask = cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert - Buttons should be disabled during processing
        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.Find(".modal-footer button.btn-success");
            confirmButton.HasAttribute("disabled").ShouldBeTrue();
        });

        // Cleanup - Complete the task
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await clickTask;
    }

    /// <summary>
    /// Verifies that a loading spinner is shown while an action is being processed.
    /// </summary>
    [Fact]
    public async Task WhenProcessing_ShouldShowSpinner()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 1, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-outline-success").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".spinner-border").Count.ShouldBe(1));

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await clickTask;
    }

    #endregion

    #region Multiple Requests Tests

    /// <summary>
    /// Verifies that the correct request is processed when multiple requests exist.
    /// </summary>
    [Fact]
    public async Task WhenMultipleRequests_ShouldProcessCorrectOne()
    {
        // Arrange
        var requests = CreateTestJoinRequests(3);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .UpdateJoinRequestStatusAsync(100, 2, RequestStatus.Approved, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        // Act - Click approve on the second request
        var approveButtons = cut.FindAll("button.btn-outline-success");
        approveButtons[1].Click();

        cut.Markup.ShouldContain("Test User 2");

        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .UpdateJoinRequestStatusAsync(100, 2, RequestStatus.Approved, Arg.Any<CancellationToken>());
    }

    #endregion
}
