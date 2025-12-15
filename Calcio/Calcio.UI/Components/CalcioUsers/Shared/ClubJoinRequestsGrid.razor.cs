using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.CalcioUsers.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class ClubJoinRequestsGrid(
    IClubJoinRequestsService clubJoinRequestService,
    NavigationManager navigationManager)
{
    [Parameter]
    public long ClubId { get; set; }

    [Parameter]
    public List<ClubJoinRequestWithUserDto> JoinRequests { get; set; } = [];

    private ClubJoinRequestWithUserDto? ConfirmingApproveRequest { get; set; }

    private ClubJoinRequestWithUserDto? ConfirmingRejectRequest { get; set; }

    private bool IsProcessing { get; set; }

    private string? ErrorMessage { get; set; }

    private void ShowApproveConfirmation(ClubJoinRequestWithUserDto request)
    {
        ErrorMessage = null;
        ConfirmingApproveRequest = request;
    }

    private void CancelApproveConfirmation()
    {
        ConfirmingApproveRequest = null;
        ErrorMessage = null;
    }

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

    private void ShowRejectConfirmation(ClubJoinRequestWithUserDto request)
    {
        ErrorMessage = null;
        ConfirmingRejectRequest = request;
    }

    private void CancelRejectConfirmation()
    {
        ConfirmingRejectRequest = null;
        ErrorMessage = null;
    }

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
