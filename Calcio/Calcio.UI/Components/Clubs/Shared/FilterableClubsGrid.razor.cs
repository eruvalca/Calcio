using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

[Authorize]
public partial class FilterableClubsGrid(
    IClubJoinRequestService clubJoinRequestService,
    NavigationManager navigationManager)
{
    [Parameter]
    public List<BaseClubDto> Clubs { get; set; } = [];

    [Parameter]
    public ClubJoinRequestDto? CurrentJoinRequest { get; set; }

    private string SearchTerm { get; set; } = string.Empty;

    private long? ConfirmingJoinClubId { get; set; }

    private bool ConfirmingCancelRequest { get; set; }

    private bool IsProcessing { get; set; }

    private string? ErrorMessage { get; set; }

    private IEnumerable<BaseClubDto> FilteredClubs
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Clubs
            : Clubs.Where(club => club.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || club.City.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || club.State.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private bool HasPendingRequest => CurrentJoinRequest?.Status == RequestStatus.Pending;

    private bool IsRejectedRequest => CurrentJoinRequest?.Status == RequestStatus.Rejected;

    private bool IsPendingClub(long clubId) => CurrentJoinRequest?.ClubId == clubId && CurrentJoinRequest?.Status == RequestStatus.Pending;

    private bool IsRejectedClub(long clubId) => CurrentJoinRequest?.ClubId == clubId && CurrentJoinRequest?.Status == RequestStatus.Rejected;

    private void ShowJoinConfirmation(long clubId)
    {
        ErrorMessage = null;
        ConfirmingJoinClubId = clubId;
    }

    private void CancelJoinConfirmation()
    {
        ConfirmingJoinClubId = null;
        ErrorMessage = null;
    }

    private async Task ConfirmJoinClub()
    {
        if (ConfirmingJoinClubId.HasValue && !IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await clubJoinRequestService.CreateJoinRequestAsync(ConfirmingJoinClubId.Value, CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingJoinClubId = null;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                notFound =>
                {
                    ErrorMessage = "The club could not be found.";
                    IsProcessing = false;
                },
                conflict =>
                {
                    ErrorMessage = "You already have a pending request to join a club.";
                    IsProcessing = false;
                },
                unauthorized =>
                {
                    ErrorMessage = "You must be logged in to request to join a club.";
                    IsProcessing = false;
                },
                error =>
                {
                    ErrorMessage = "An unexpected error occurred. Please try again.";
                    IsProcessing = false;
                });
        }
    }

    private void ShowCancelConfirmation()
    {
        ErrorMessage = null;
        ConfirmingCancelRequest = true;
    }

    private void DismissCancelConfirmation()
    {
        ConfirmingCancelRequest = false;
        ErrorMessage = null;
    }

    private async Task ConfirmCancelRequest()
    {
        if (!IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await clubJoinRequestService.CancelJoinRequestAsync(CancellationToken);

            result.Switch(
                success =>
                {
                    ConfirmingCancelRequest = false;
                    IsProcessing = false;
                    navigationManager.Refresh();
                },
                notFound =>
                {
                    ErrorMessage = "No pending request found to cancel.";
                    IsProcessing = false;
                },
                unauthorized =>
                {
                    ErrorMessage = "You must be logged in to cancel a request.";
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
