using Bunit;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Components.Clubs.Shared;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Calcio.UnitTests.Components.Clubs.Shared;

/// <summary>
/// Contains unit tests for C lu bM em be rs hi pP an el behavior.
/// </summary>
public sealed class ClubMembershipPanelTests : BunitContext
{
    /// <summary>
    /// Stores test state for c lu bs se rv ic e.
    /// </summary>
    private readonly IClubsService _clubsService;
    /// <summary>
    /// Stores test state for a cc ou nt se rv ic e.
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
    }
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

        var cut = Render<ClubMembershipPanel>(parameters => parameters
            .Add(p => p.AllClubs, new List<BaseClubDto>())
            .Add(p => p.CurrentJoinRequest, null));

        cut.Find("input[id='Input.Name']").Change("New Club");
        cut.Find("input[id='Input.City']").Change("City");
        cut.Find("select[id='Input.State']").Change("TX");

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
    /// <summary>
    /// Verifies the WhenCreateClubFails_ShouldShowErrorMessage scenario.
    /// </summary>

    [Fact]
    public void WhenCreateClubFails_ShouldShowErrorMessage()
    {
        // Arrange
        _clubsService.CreateClubAsync(Arg.Any<CreateClubDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ServiceResult<ClubCreatedDto>(ServiceProblem.Conflict())));

        var cut = Render<ClubMembershipPanel>(parameters => parameters
            .Add(p => p.AllClubs, new List<BaseClubDto>())
            .Add(p => p.CurrentJoinRequest, null));

        cut.Find("input[id='Input.Name']").Change("Existing Club");
        cut.Find("input[id='Input.City']").Change("City");
        cut.Find("select[id='Input.State']").Change("TX");

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
}
