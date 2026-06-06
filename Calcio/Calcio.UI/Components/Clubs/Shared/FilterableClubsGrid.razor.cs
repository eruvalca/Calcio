using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Results;
using Calcio.Shared.Services.ClubJoinRequests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

[Authorize]
/// <summary>
/// Displays clubs with search and join-request actions for the current user.
/// </summary>
/// <param name="clubJoinRequestService">The service used to create or cancel join requests.</param>
/// <param name="navigationManager">The navigation manager used to refresh the page after actions.</param>
public partial class FilterableClubsGrid(
    IClubJoinRequestsService clubJoinRequestService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Gets or sets the clubs shown in the grid.
    /// </summary>
    [Parameter]
    public List<BaseClubDto> Clubs { get; set; } = [];

    /// <summary>
    /// Gets or sets the current user's active join request, if any.
    /// </summary>
    [Parameter]
    public ClubJoinRequestDto? CurrentJoinRequest { get; set; }

    /// <summary>
    /// Gets or sets the current search term used to filter clubs.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the club ID pending join confirmation.
    /// </summary>
    private long? ConfirmingJoinClubId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cancel confirmation is displayed.
    /// </summary>
    private bool ConfirmingCancelRequest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an action is currently being processed.
    /// </summary>
    private bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the current error message displayed to the user.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets clubs matching the current search term.
    /// </summary>
    private IEnumerable<BaseClubDto> FilteredClubs
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Clubs
            : Clubs.Where(club => club.Name.ContainsIgnoreCase(SearchTerm)
                || club.City.ContainsIgnoreCase(SearchTerm)
                || club.State.ContainsIgnoreCase(SearchTerm));

    /// <summary>
    /// Gets a value indicating whether the user has a pending join request.
    /// </summary>
    private bool HasPendingRequest => CurrentJoinRequest?.Status == RequestStatus.Pending;

    /// <summary>
    /// Determines whether the specified club is associated with a pending join request.
    /// </summary>
    /// <param name="clubId">The club identifier to evaluate.</param>
    /// <returns><see langword="true"/> when the club has a pending request; otherwise, <see langword="false"/>.</returns>
    private bool IsPendingClub(long clubId) => CurrentJoinRequest?.ClubId == clubId && CurrentJoinRequest?.Status == RequestStatus.Pending;

    /// <summary>
    /// Determines whether the specified club is associated with a rejected join request.
    /// </summary>
    /// <param name="clubId">The club identifier to evaluate.</param>
    /// <returns><see langword="true"/> when the club has a rejected request; otherwise, <see langword="false"/>.</returns>
    private bool IsRejectedClub(long clubId) => CurrentJoinRequest?.ClubId == clubId && CurrentJoinRequest?.Status == RequestStatus.Rejected;

    /// <summary>
    /// Opens the join confirmation dialog for a club.
    /// </summary>
    /// <param name="clubId">The club identifier being joined.</param>
    private void ShowJoinConfirmation(long clubId)
    {
        ErrorMessage = null;
        ConfirmingJoinClubId = clubId;
    }

    /// <summary>
    /// Closes the join confirmation dialog.
    /// </summary>
    private void CancelJoinConfirmation()
    {
        ConfirmingJoinClubId = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Submits a join request for the selected club.
    /// </summary>
    /// <returns>A task that completes when request processing finishes.</returns>
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
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "The club could not be found.",
                        ServiceProblemKind.Conflict => "You already have a pending request to join a club.",
                        ServiceProblemKind.Forbidden => "You do not have permission to perform this action.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }

    /// <summary>
    /// Opens the confirmation dialog for canceling the current join request.
    /// </summary>
    private void ShowCancelConfirmation()
    {
        ErrorMessage = null;
        ConfirmingCancelRequest = true;
    }

    /// <summary>
    /// Dismisses the cancel-request confirmation dialog.
    /// </summary>
    private void DismissCancelConfirmation()
    {
        ConfirmingCancelRequest = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Cancels the current pending join request.
    /// </summary>
    /// <returns>A task that completes when cancellation processing finishes.</returns>
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
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "No pending request found to cancel.",
                        ServiceProblemKind.Forbidden => "You are not authorized to cancel this request.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }
}
