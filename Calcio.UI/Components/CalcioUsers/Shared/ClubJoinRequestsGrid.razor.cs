using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.CalcioUsers.Shared;

[Authorize(Roles = Roles.ClubAdmin)]
/// <summary>
/// Displays club join requests and allows club administrators to approve or reject them.
/// </summary>
/// <param name="clubJoinRequestService">The service used to update join request statuses.</param>
/// <param name="navigationManager">The navigation manager used to refresh the page after updates.</param>
public partial class ClubJoinRequestsGrid(
    IClubJoinRequestsService clubJoinRequestService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Gets or sets the club identifier for the displayed requests.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the join requests currently displayed in the grid.
    /// </summary>
    [Parameter]
    public List<ClubJoinRequestWithUserDto> JoinRequests { get; set; } = [];

    /// <summary>
    /// Gets or sets the request awaiting approve confirmation.
    /// </summary>
    private ClubJoinRequestWithUserDto? ConfirmingApproveRequest { get; set; }

    /// <summary>
    /// Gets or sets the request awaiting reject confirmation.
    /// </summary>
    private ClubJoinRequestWithUserDto? ConfirmingRejectRequest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a request action is being processed.
    /// </summary>
    private bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets an error message displayed for failed actions.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Opens the approve confirmation dialog for a request.
    /// </summary>
    /// <param name="request">The request targeted for approval.</param>
    private void ShowApproveConfirmation(ClubJoinRequestWithUserDto request)
    {
        ErrorMessage = null;
        ConfirmingApproveRequest = request;
    }

    /// <summary>
    /// Closes the approve confirmation dialog.
    /// </summary>
    private void CancelApproveConfirmation()
    {
        ConfirmingApproveRequest = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Approves the currently selected join request.
    /// </summary>
    /// <returns>A task that completes when approval processing finishes.</returns>
    private async Task ConfirmApprove()
    {
        if (ConfirmingApproveRequest is not null && !IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await clubJoinRequestService.UpdateJoinRequestStatusAsync(
                ClubId,
                ConfirmingApproveRequest.ClubJoinRequestId,
                RequestStatus.Approved,
                CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingApproveRequest = null;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "The join request could not be found.",
                        ServiceProblemKind.Forbidden => "You are not authorized to approve this request.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }

    /// <summary>
    /// Opens the reject confirmation dialog for a request.
    /// </summary>
    /// <param name="request">The request targeted for rejection.</param>
    private void ShowRejectConfirmation(ClubJoinRequestWithUserDto request)
    {
        ErrorMessage = null;
        ConfirmingRejectRequest = request;
    }

    /// <summary>
    /// Closes the reject confirmation dialog.
    /// </summary>
    private void CancelRejectConfirmation()
    {
        ConfirmingRejectRequest = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Rejects the currently selected join request.
    /// </summary>
    /// <returns>A task that completes when rejection processing finishes.</returns>
    private async Task ConfirmReject()
    {
        if (ConfirmingRejectRequest is not null && !IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await clubJoinRequestService.UpdateJoinRequestStatusAsync(
                ClubId,
                ConfirmingRejectRequest.ClubJoinRequestId,
                RequestStatus.Rejected,
                CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingRejectRequest = null;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "The join request could not be found.",
                        ServiceProblemKind.Forbidden => "You are not authorized to reject this request.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }
}
