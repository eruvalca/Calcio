using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

[Authorize]
public partial class Clubs(
    IClubsService clubsService,
    IClubJoinRequestsService clubJoinRequestsService,
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private LeaveClubInputModel LeaveClubInput { get; set; } = default!;

    private long UserId { get; set; } = default;
    private List<BaseClubDto> UserClubs { get; set; } = [];
    private List<BaseClubDto> AllClubs { get; set; } = [];
    private ClubJoinRequestDto? CurrentJoinRequest { get; set; }
    private bool IsClubAdmin { get; set; }

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

    private sealed class LeaveClubInputModel
    {
        public long ClubId { get; set; }
    }
}
