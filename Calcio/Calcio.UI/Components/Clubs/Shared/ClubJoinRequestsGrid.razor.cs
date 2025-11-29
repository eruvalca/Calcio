using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

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

            var result = await clubJoinRequestService.ApproveJoinRequestAsync(
                ClubId,
                ConfirmingApproveRequest.ClubJoinRequestId,
                CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingApproveRequest = null;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                notFound =>
                {
                    ErrorMessage = "The join request could not be found.";
                    IsProcessing = false;
                },
                unauthorized =>
                {
                    ErrorMessage = "You are not authorized to approve this request.";
                    IsProcessing = false;
                },
                error =>
                {
                    ErrorMessage = "An unexpected error occurred. Please try again.";
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

            var result = await clubJoinRequestService.RejectJoinRequestAsync(
                ClubId,
                ConfirmingRejectRequest.ClubJoinRequestId,
                CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingRejectRequest = null;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                notFound =>
                {
                    ErrorMessage = "The join request could not be found.";
                    IsProcessing = false;
                },
                unauthorized =>
                {
                    ErrorMessage = "You are not authorized to reject this request.";
                    IsProcessing = false;
                },
                error =>
                {
                    ErrorMessage = "An unexpected error occurred. Please try again.";
                    IsProcessing = false;
                });
        }
    }
}
