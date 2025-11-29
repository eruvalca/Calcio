using Bunit;

using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.UI.Components.CalcioUsers;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using OneOf.Types;

using Shouldly;

namespace Calcio.UnitTests.Components.CalcioUsers.Shared;

/// <summary>
/// Unit tests for the ClubMembersGrid Blazor component using bUnit.
/// </summary>
public sealed class ClubMembersGridTests : BunitContext
{
    private readonly ICalcioUsersService _mockCalcioUsersService;

    public ClubMembersGridTests()
    {
        _mockCalcioUsersService = Substitute.For<ICalcioUsersService>();
        Services.AddSingleton(_mockCalcioUsersService);

        // QuickGrid uses JS interop for virtualization
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Set up authorization for ClubAdmin role requirement
        var authContext = AddAuthorization();
        authContext.SetAuthorized("TestUser");
    }

    #region Helper Methods

    private static List<ClubMemberDto> CreateTestMembers(int count = 2, bool includeAdmin = true)
    {
        var members = new List<ClubMemberDto>();

        if (includeAdmin)
        {
            members.Add(new ClubMemberDto(
                UserId: 2,
                FullName: "Admin User",
                Email: "admin@test.com",
                IsClubAdmin: true));
        }

        for (var i = 1; i <= count; i++)
        {
            members.Add(new ClubMemberDto(
                UserId: 100 + i,
                FullName: $"Member {i}",
                Email: $"member{i}@test.com",
                IsClubAdmin: false));
        }

        return members;
    }

    private IRenderedComponent<ClubMembersGrid> RenderGrid(long clubId = 100)
        => Render<ClubMembersGrid>(parameters => parameters
            .Add(p => p.ClubId, clubId));

    #endregion

    #region Loading State Tests

    [Fact]
    public void WhenLoading_ShouldDisplaySpinner()
    {
        // Arrange - Setup mock to never complete
        var tcs = new TaskCompletionSource<ServiceResult<List<ClubMemberDto>>>();
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = RenderGrid();

        // Assert
        cut.Find(".spinner-border").ShouldNotBeNull();
        cut.FindAll("table").ShouldBeEmpty();
    }

    #endregion

    #region Initial Rendering Tests

    [Fact]
    public void WhenNoMembers_ShouldDisplayEmptyMessage()
    {
        // Arrange
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(new List<ClubMemberDto>()));

        // Act
        var cut = RenderGrid();

        // Wait for async load to complete
        cut.WaitForAssertion(() =>
        {
            var emptyMessage = cut.Find(".text-muted");
            emptyMessage.TextContent.ShouldBe("No members in this club.");
        });
    }

    [Fact]
    public void WhenMembersExist_ShouldDisplayGrid()
    {
        // Arrange
        var members = CreateTestMembers(2);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("table").ShouldNotBeNull();

            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // 1 admin + 2 members

            cut.Markup.ShouldContain("Admin User");
            cut.Markup.ShouldContain("Member 1");
            cut.Markup.ShouldContain("Member 2");
        });
    }

    [Fact]
    public void WhenMembersExist_ShouldDisplayCorrectRoleBadges()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var adminBadge = cut.Find(".badge.bg-primary");
            adminBadge.TextContent.ShouldBe("Club Admin");

            var memberBadge = cut.Find(".badge.bg-secondary");
            memberBadge.TextContent.ShouldBe("Member");
        });
    }

    [Fact]
    public void WhenMembersExist_ShouldNotShowRemoveButtonForAdmins()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            // Only 1 remove button for the non-admin member
            var removeButtons = cut.FindAll("button.btn-outline-danger");
            removeButtons.Count.ShouldBe(1);
        });
    }

    #endregion

    #region Error State Tests

    [Fact]
    public void WhenLoadReturnsForbidden_ShouldDisplayErrorMessage()
    {
        // Arrange
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(ServiceProblem.Forbidden()));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    [Fact]
    public void WhenLoadReturnsServerError_ShouldDisplayErrorMessage()
    {
        // Arrange
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(ServiceProblem.ServerError()));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Remove Modal Tests

    [Fact]
    public void WhenRemoveClicked_ShouldShowConfirmationModal()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        // Act
        cut.Find("button.btn-outline-danger").Click();

        // Assert
        var modal = cut.Find(".modal");
        modal.ShouldNotBeNull();
        cut.Markup.ShouldContain("Remove Club Member");
        cut.Markup.ShouldContain("Member 1");
    }

    [Fact]
    public void WhenCancelClicked_ShouldHideModal()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        cut.Find(".modal-footer button.btn-secondary").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    [Fact]
    public void WhenCloseButtonClicked_ShouldHideModal()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        cut.Find(".modal-header .btn-close").Click();

        // Assert
        cut.FindAll(".modal").ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenRemoveConfirmed_ShouldCallService()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        await _mockCalcioUsersService.Received(1)
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenRemoveSucceeds_ShouldRemoveMemberFromList()
    {
        // Arrange
        var members = CreateTestMembers(2);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(new Success()));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        // Click first non-admin member's remove button
        cut.FindAll("button.btn-outline-danger")[0].Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".modal").ShouldBeEmpty();
            // Should now only have 1 remove button (for the remaining non-admin)
            cut.FindAll("button.btn-outline-danger").Count.ShouldBe(1);
        });
    }

    [Fact]
    public async Task WhenRemoveReturnsNotFound_ShouldDisplayError()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.NotFound()));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".modal .alert-danger");
            alert.TextContent.ShouldContain("could not be found");
        });
    }

    [Fact]
    public async Task WhenRemoveReturnsForbidden_ShouldDisplayError()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.Forbidden()));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".modal .alert-danger");
            alert.TextContent.ShouldContain("not authorized");
        });
    }

    [Fact]
    public async Task WhenRemoveReturnsServerError_ShouldDisplayGenericError()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<Success>(ServiceProblem.ServerError()));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        await cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".modal .alert-danger");
            alert.TextContent.ShouldContain("unexpected error");
        });
    }

    #endregion

    #region Search/Filter Tests

    [Fact]
    public void WhenSearchTermIsEmpty_ShouldDisplayAllMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        // Act
        var cut = RenderGrid();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(4); // 1 admin + 3 members
        });
    }

    [Fact]
    public void WhenSearchByFullName_ShouldFilterMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 4);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("Member 1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Member 1");
            cut.Markup.ShouldNotContain("Member 2");
            cut.Markup.ShouldNotContain("Member 3");
            cut.Markup.ShouldNotContain("Admin User");
        });
    }

    [Fact]
    public void WhenSearchByEmail_ShouldFilterMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 4);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("member2@test.com");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Member 2");
            cut.Markup.ShouldNotContain("Member 1");
            cut.Markup.ShouldNotContain("Member 3");
        });
    }

    [Fact]
    public void WhenSearchByPartialName_ShouldFilterMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 4);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("Member");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(3); // All 3 non-admin members match "Member"
            cut.Markup.ShouldNotContain("Admin User");
        });
    }

    [Fact]
    public void WhenSearchIsCaseInsensitive_ShouldFilterMembers()
    {
        // Arrange
        var members = CreateTestMembers(2);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("ADMIN");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(1);
            cut.Markup.ShouldContain("Admin User");
        });
    }

    [Fact]
    public void WhenSearchMatchesNoMembers_ShouldDisplayEmptyGrid()
    {
        // Arrange
        var members = CreateTestMembers(2);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 3);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("nonexistent");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenSearchCleared_ShouldDisplayAllMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 4);

        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("Member 1");

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        // Act
        searchInput.Input(string.Empty);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(4); // All members displayed again
        });
    }

    [Fact]
    public void WhenSearchByPartialEmail_ShouldFilterMembers()
    {
        // Arrange
        var members = CreateTestMembers(3);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 4);

        // Act
        var searchInput = cut.Find("#MemberSearch");
        searchInput.Input("@test.com");

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Count.ShouldBe(4); // All members have @test.com in email
        });
    }

    #endregion

    #region Processing State Tests

    [Fact]
    public async Task WhenProcessing_ShouldDisableButtons()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.Find(".modal-footer button.btn-danger");
            confirmButton.HasAttribute("disabled").ShouldBeTrue();
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await clickTask;
    }

    [Fact]
    public async Task WhenProcessing_ShouldShowSpinner()
    {
        // Arrange
        var members = CreateTestMembers(1);
        _mockCalcioUsersService
            .GetClubMembersAsync(100, Arg.Any<CancellationToken>())
            .Returns(new ServiceResult<List<ClubMemberDto>>(members));

        var tcs = new TaskCompletionSource<ServiceResult<Success>>();
        _mockCalcioUsersService
            .RemoveClubMemberAsync(100, 101, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderGrid();

        cut.WaitForState(() => cut.FindAll("button.btn-outline-danger").Count > 0);

        cut.Find("button.btn-outline-danger").Click();

        // Act
        var clickTask = cut.Find(".modal-footer button.btn-danger").ClickAsync(new());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".modal .spinner-border").Count.ShouldBe(1);
        });

        // Cleanup
        tcs.SetResult(new ServiceResult<Success>(new Success()));
        await clickTask;
    }

    #endregion
}
