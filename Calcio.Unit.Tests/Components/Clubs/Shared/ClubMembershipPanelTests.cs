using Bunit;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Components.Clubs.Shared;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Components.Clubs.Shared;

/// <summary>
/// Contains unit tests for ClubMembershipPanel behavior.
/// </summary>
public sealed class ClubMembershipPanelTests : BunitContext
{
    /// <summary>
    /// Stores the mock clubs service used throughout tests.
    /// </summary>
    private readonly IClubsService _clubsService;

    /// <summary>
    /// Stores the mock account service used throughout tests.
    /// </summary>
    private readonly IAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClubMembershipPanelTests"/> class.
    /// </summary>
    public ClubMembershipPanelTests()
    {
        _clubsService = Substitute.For<IClubsService>();
        Services.AddSingleton(_clubsService);
        Services.AddSingleton(TimeProvider.System);

        _accountService = Substitute.For<IAccountService>();
        _accountService.RefreshSignInAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<OneOf.Types.Success>(new OneOf.Types.Success())));
        Services.AddSingleton(_accountService);

        // IClubJoinRequestsService is required when AllClubs is non-empty (FilterableClubsGrid renders)
        Services.AddSingleton(Substitute.For<IClubJoinRequestsService>());

        var authContext = AddAuthorization();
        authContext.SetAuthorized("TestUser");
    }

    #region Helper Methods

    private IRenderedComponent<ClubMembershipPanel> RenderPanel(
        List<BaseClubDto>? clubs = null,
        ClubJoinRequestDto? joinRequest = null)
        => Render<ClubMembershipPanel>(parameters => parameters
            .Add(p => p.AllClubs, clubs ?? [])
            .Add(p => p.CurrentJoinRequest, joinRequest));

    private void FillValidClubData(IRenderedComponent<ClubMembershipPanel> cut)
    {
        cut.Find("input[id='Input.Name']").Change("New Club");
        cut.Find("input[id='Input.City']").Change("City");
        cut.Find("select[id='Input.State']").Change("TX");
    }

    #endregion

    #region Create Club Success Tests

    /// <summary>
    /// Verifies the WhenCreateClubSucceeds_ShouldUpdateStateAndShowManageButton scenario.
    /// </summary>
    [Fact]
    public void WhenCreateClubSucceeds_ShouldUpdateStateAndShowManageButton()
    {
        // Arrange
        var created = new ClubCreatedDto(42, "New Club");
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(created)));

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert.alert-success").TextContent.ShouldContain("Club 'New Club' created.");
            cut.Find("a.btn.btn-primary").GetAttribute("href").ShouldBe("/clubs/42");
        });

        _accountService.Received(1).RefreshSignInAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create Club Error Tests

    /// <summary>
    /// Verifies the WhenCreateClubFails_Conflict_ShouldShowConflictMessage scenario.
    /// </summary>
    [Fact]
    public void WhenCreateClubFails_Conflict_ShouldShowConflictMessage()
    {
        // Arrange
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.Conflict())));

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldContain("That club already exists.");
        });

        _accountService.DidNotReceive().RefreshSignInAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies the WhenCreateClubFails_Forbidden_ShouldShowForbiddenMessage scenario.
    /// </summary>
    [Fact]
    public void WhenCreateClubFails_Forbidden_ShouldShowForbiddenMessage()
    {
        // Arrange
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.Forbidden())));

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldContain("do not have permission");
        });

        _accountService.DidNotReceive().RefreshSignInAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies the WhenCreateClubFails_BadRequest_ShouldShowBadRequestMessage scenario.
    /// </summary>
    [Fact]
    public void WhenCreateClubFails_BadRequest_ShouldShowBadRequestMessage()
    {
        // Arrange
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.BadRequest("Invalid data"))));

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldContain("Invalid club details.");
        });

        _accountService.DidNotReceive().RefreshSignInAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies the WhenCreateClubFails_ServerError_ShouldShowGenericError scenario.
    /// </summary>
    [Fact]
    public void WhenCreateClubFails_ServerError_ShouldShowGenericError()
    {
        // Arrange
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.ServerError())));

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });

        _accountService.DidNotReceive().RefreshSignInAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Submitting State Tests

    /// <summary>
    /// Verifies that the submit button is disabled and shows a spinner while the request is in flight.
    /// </summary>
    [Fact]
    public void WhenSubmitting_ShouldDisableSubmitButtonAndShowSpinner()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ServiceResult<ClubCreatedDto>>();
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderPanel();
        FillValidClubData(cut);

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find(".spinner-border").ShouldNotBeNull();
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.ServerError()));
    }

    #endregion

    #region Join Request State Tests

    /// <summary>
    /// Verifies that the create-club form is hidden when the user has a pending join request.
    /// </summary>
    [Fact]
    public void WhenRendered_WithPendingJoinRequest_ShouldHideCreateClubForm()
    {
        // Arrange
        var pendingRequest = new ClubJoinRequestDto(1, 100, 1, RequestStatus.Pending);

        // Act
        var cut = RenderPanel(joinRequest: pendingRequest);

        // Assert - the create club section must not appear when a request is already pending
        var createClubHeaders = cut.FindAll("h5").Where(h => h.TextContent.Contains("Create a club")).ToList();
        createClubHeaders.ShouldBeEmpty();
        cut.FindAll("form").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that the create-club form is visible when the user has no active join request.
    /// </summary>
    [Fact]
    public void WhenRendered_WithNoJoinRequest_ShouldShowCreateClubForm()
    {
        // Act
        var cut = RenderPanel(joinRequest: null);

        // Assert
        cut.FindAll("h5").ShouldContain(h => h.TextContent.Contains("Create a club"));
        cut.Find("form").ShouldNotBeNull();
    }

    #endregion
}
