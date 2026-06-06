using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Clubs.
/// </summary>
/// <param name="clubsService">The clubs Service.</param>
/// <param name="clubJoinRequestsService">The club Join Requests Service.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
[Authorize]
/// <summary>
/// Represents the clubs class.
/// </summary>
public partial class Clubs(
    IClubsService clubsService,
    IClubJoinRequestsService clubJoinRequestsService,
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Leave Club Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the leave club input.
    /// </summary>
    private LeaveClubInputModel LeaveClubInput { get; set; } = default!;

    /// <summary>
    /// Gets or sets the User Id.
    /// </summary>
    private long UserId { get; set; } = default;
    /// <summary>
    /// Gets or sets the User Clubs.
    /// </summary>
    private List<BaseClubDto> UserClubs { get; set; } = [];
    /// <summary>
    /// Gets or sets the All Clubs.
    /// </summary>
    private List<BaseClubDto> AllClubs { get; set; } = [];
    /// <summary>
    /// Gets or sets the Current Join Request.
    /// </summary>
    private ClubJoinRequestDto? CurrentJoinRequest { get; set; }
    /// <summary>
    /// Gets or sets the Is Club Admin.
    /// </summary>
    private bool IsClubAdmin { get; set; }

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        UserId = long.TryParse(userManager.GetUserId(HttpContext.User), out var userId)
            ? userId
            : throw new InvalidOperationException("Current user cannot be null.");

        LeaveClubInput ??= new();

        var user = await userManager.FindByIdAsync(UserId.ToString());
        IsClubAdmin = user is not null && await userManager.IsInRoleAsync(user, Roles.ClubAdmin);

        var userClubsResult = await clubsService.GetUserClubsAsync(CancellationToken);
        userClubsResult.Switch(
            clubs => UserClubs = clubs,
            problem => UserClubs = []);

        if (UserClubs.Count == 0)
        {
            var joinRequestResult = await clubJoinRequestsService.GetRequestForCurrentUserAsync(CancellationToken);
            joinRequestResult.Switch(
                request => CurrentJoinRequest = request,
                problem => CurrentJoinRequest = null);

            var allClubsResult = await clubsService.GetAllClubsForBrowsingAsync(CancellationToken);
            allClubsResult.Switch(
                clubs => AllClubs = clubs,
                problem => AllClubs = []);
        }
    }

    /// <summary>
    /// Executes the Leave Club operation.
    /// </summary>
    /// <param name="editContext">The edit Context.</param>
    /// <returns>The operation result.</returns>
    public async Task LeaveClub(EditContext editContext)
    {
        if (IsClubAdmin || LeaveClubInput.ClubId <= 0)
        {
            return;
        }

        var clubToLeave = UserClubs.FirstOrDefault(c => c.Id == LeaveClubInput.ClubId);
        if (clubToLeave is null)
        {
            return;
        }

        var result = await clubsService.LeaveClubAsync(LeaveClubInput.ClubId, CancellationToken);

        await result.Match(
            async _ =>
            {
                // Refresh the sign-in to update the user's roles/claims
                var user = await userManager.FindByIdAsync(UserId.ToString());
                if (user is not null)
                {
                    await signInManager.RefreshSignInAsync(user);
                }

                redirectManager.RedirectToWithStatus("Account/Manage/Clubs", $"You have left '{clubToLeave.Name}'.", HttpContext);
            },
            problem =>
            {
                var message = problem.Kind switch
                {
                    ServiceProblemKind.NotFound => "That club could not be found.",
                    ServiceProblemKind.Forbidden => "You do not have permission to leave that club.",
                    ServiceProblemKind.Conflict => "You cannot leave that club right now.",
                    ServiceProblemKind.BadRequest => "Invalid request.",
                    _ => "An unexpected error occurred while leaving the club."
                };

                redirectManager.RedirectToWithStatus("Account/Manage/Clubs", message, HttpContext);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Represents the Leave Club Input Model.
    /// </summary>
    private sealed class LeaveClubInputModel
    {
        /// <summary>
        /// Gets or sets the Club Id.
        /// </summary>
        public long ClubId { get; set; }
    }
}
