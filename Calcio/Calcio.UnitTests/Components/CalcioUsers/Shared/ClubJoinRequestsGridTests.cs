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
/// 
/// KEY BUNIT CONCEPTS:
/// 
/// 1. BunitContext - The core bUnit class that provides the test environment.
///    It manages component lifecycle, services, and rendering.
///    
/// 2. Render&lt;T&gt; - Renders a component and returns an IRenderedComponent&lt;T&gt;
///    that you can use to interact with and assert against the component.
///    
/// 3. Find/FindAll - CSS selector-based methods to locate elements in the rendered markup.
/// 
/// 4. Click/Change/Submit - Methods to simulate user interactions.
/// 
/// 5. WaitForState/WaitForAssertion - Methods to handle async operations in components.
/// 
/// 6. Services - DI container for injecting mock services into components.
/// </summary>
public sealed class ClubJoinRequestsGridTests : BunitContext
{
    // Mock dependencies - we use NSubstitute to create test doubles
    private readonly IClubJoinRequestsService _mockClubJoinRequestService;

    public ClubJoinRequestsGridTests()
    {
        // BEST PRACTICE: Set up mocks in constructor for reuse across tests
        _mockClubJoinRequestService = Substitute.For<IClubJoinRequestsService>();

        // BUNIT FEATURE: Register services in the test context's DI container
        // These will be injected into components just like in the real app
        Services.AddSingleton(_mockClubJoinRequestService);

        // BUNIT FEATURE: Configure JSInterop to handle calls from third-party components
        // QuickGrid uses JS interop for virtualization. We set up a catch-all handler
        // that allows any JS call to succeed. This is common when testing components
        // that use JS-dependent third-party libraries.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Helper Methods

    /// <summary>
    /// Creates test data for join requests.
    /// BEST PRACTICE: Use factory methods to create test data consistently.
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
    /// BEST PRACTICE: Create helper methods for common rendering scenarios.
    /// </summary>
    private IRenderedComponent<ClubJoinRequestsGrid> RenderGrid(
        long clubId = 100,
        List<ClubJoinRequestWithUserDto>? joinRequests = null)
            // BUNIT FEATURE: Parameters can be passed as a lambda
            // This is type-safe and catches errors at compile time
            => Render<ClubJoinRequestsGrid>(parameters => parameters
                .Add(p => p.ClubId, clubId)
                .Add(p => p.JoinRequests, joinRequests ?? []));

    #endregion

    #region Initial Rendering Tests

    /// <summary>
    /// TEST: Verify the component renders the empty state correctly.
    /// CONCEPT: Testing initial render state with no data.
    /// </summary>
    [Fact]
    public void WhenNoJoinRequests_ShouldDisplayEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderGrid(joinRequests: []);

        // Assert
        // BUNIT FEATURE: Find uses CSS selectors just like JavaScript
        var emptyMessage = cut.Find(".text-muted");
        emptyMessage.TextContent.ShouldBe("No pending join requests.");

        // BEST PRACTICE: Also verify things that should NOT be present
        // FindAll returns empty collection if nothing matches (unlike Find which throws)
        cut.FindAll("table").ShouldBeEmpty();
    }

    /// <summary>
    /// TEST: Verify the component renders join requests in a grid.
    /// CONCEPT: Testing that data is rendered correctly.
    /// </summary>
    [Fact]
    public void WhenJoinRequestsExist_ShouldDisplayGrid()
    {
        // Arrange
        var requests = CreateTestJoinRequests(2);

        // Act
        var cut = RenderGrid(joinRequests: requests);

        // Assert
        // Verify the grid is rendered
        cut.Find("table").ShouldNotBeNull();

        // BUNIT FEATURE: FindAll returns all matching elements
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);

        // Verify first request's data is displayed
        cut.Markup.ShouldContain("Test User 1");
        cut.Markup.ShouldContain("user1@test.com");
    }

    /// <summary>
    /// TEST: Verify each request has approve and reject buttons.
    /// CONCEPT: Testing UI elements exist for interaction.
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
    /// TEST: Verify clicking approve shows confirmation modal.
    /// CONCEPT: Testing user interaction triggers UI changes.
    /// </summary>
    [Fact]
    public void WhenApproveClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(joinRequests: requests);

        // Act
        // BUNIT FEATURE: Click simulates a user clicking the element
        cut.Find("button.btn-outline-success").Click();

        // Assert
        // The modal should now be visible
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();

        // Verify modal content
        cut.Markup.ShouldContain("Approve Join Request");
        cut.Markup.ShouldContain("Test User 1");
    }

    /// <summary>
    /// TEST: Verify cancel button closes the approve modal.
    /// CONCEPT: Testing modal dismissal.
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
    /// TEST: Verify clicking X button closes the approve modal.
    /// CONCEPT: Testing alternative modal dismissal.
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
    /// TEST: Verify confirming approval calls the service.
    /// CONCEPT: Testing component calls injected service correctly.
    /// </summary>
    [Fact]
    public async Task WhenApproveConfirmed_ShouldCallService()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        // Setup mock to return success
        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        // Open the modal
        cut.Find("button.btn-outline-success").Click();

        // Act
        // Find and click the confirm button (the success-styled button in footer)
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        // BEST PRACTICE: Verify the service was called with correct parameters
        await _mockClubJoinRequestService.Received(1)
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST: Verify error message displays when approval fails with NotFound.
    /// CONCEPT: Testing error handling and UI feedback.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.NotFound()));

        cut.Find("button.btn-outline-success").Click();

        // Act
        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        // BUNIT FEATURE: WaitForAssertion handles async state updates
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("could not be found");
        });
    }

    /// <summary>
    /// TEST: Verify error message displays when approval fails with Forbidden.
    /// CONCEPT: Testing different error scenarios.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsForbidden_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
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
    /// TEST: Verify error message displays when approval fails with generic error.
    /// CONCEPT: Testing fallback error handling.
    /// </summary>
    [Fact]
    public async Task WhenApproveReturnsServerError_ShouldDisplayGenericError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
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
    /// TEST: Verify clicking reject shows confirmation modal.
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
    /// TEST: Verify cancel button closes the reject modal.
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
    /// TEST: Verify confirming rejection calls the service.
    /// </summary>
    [Fact]
    public async Task WhenRejectConfirmed_ShouldCallService()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .RejectJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        await _mockClubJoinRequestService.Received(1)
            .RejectJoinRequestAsync(100, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST: Verify error message displays when rejection fails.
    /// </summary>
    [Fact]
    public async Task WhenRejectReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .RejectJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
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
    /// TEST: Verify buttons are disabled while processing.
    /// CONCEPT: Testing UI state during async operations.
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
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
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
    /// TEST: Verify spinner is shown while processing.
    /// CONCEPT: Testing loading indicators.
    /// </summary>
    [Fact]
    public async Task WhenProcessing_ShouldShowSpinner()
    {
        // Arrange
        var requests = CreateTestJoinRequests(1);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 1, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        cut.Find("button.btn-outline-success").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".spinner-border").Count.ShouldBe(1);
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await clickTask;
    }

    #endregion

    #region Multiple Requests Tests

    /// <summary>
    /// TEST: Verify correct request is processed when multiple exist.
    /// CONCEPT: Testing data binding with multiple items.
    /// </summary>
    [Fact]
    public async Task WhenMultipleRequests_ShouldProcessCorrectOne()
    {
        // Arrange
        var requests = CreateTestJoinRequests(3);
        var cut = RenderGrid(clubId: 100, joinRequests: requests);

        _mockClubJoinRequestService
            .ApproveJoinRequestAsync(100, 2, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        // Act - Click approve on the second request
        var approveButtons = cut.FindAll("button.btn-outline-success");
        approveButtons[1].Click(); // Second request (index 1)

        // Verify the modal shows the correct user
        cut.Markup.ShouldContain("Test User 2");

        await cut.Find(".modal-footer button.btn-success").ClickAsync(new());

        // Assert - Verify the correct request ID was used
        await _mockClubJoinRequestService.Received(1)
            .ApproveJoinRequestAsync(100, 2, Arg.Any<CancellationToken>());
    }

    #endregion
}
