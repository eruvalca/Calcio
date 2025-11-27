using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.CalcioUsers;

[Authorize(Roles = "ClubAdmin")]
public partial class ClubMembersGrid(ICalcioUsersService calcioUsersService)
{
    [Parameter]
    public required long ClubId { get; set; }

    private List<ClubMemberDto> Members { get; set; } = [];

    private string SearchTerm { get; set; } = string.Empty;

    private ClubMemberDto? ConfirmingRemoveMember { get; set; }

    private bool IsLoading { get; set; } = true;

    private bool IsProcessing { get; set; }

    private string? LoadErrorMessage { get; set; }

    private string? ErrorMessage { get; set; }

    private IEnumerable<ClubMemberDto> FilteredMembers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Members
            : Members.Where(member => member.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || member.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
        => await LoadMembersAsync();

    private async Task LoadMembersAsync()
    {
        IsLoading = true;
        LoadErrorMessage = null;

        var result = await calcioUsersService.GetClubMembersAsync(ClubId, CancellationToken);

        result.Switch(
            members =>
            {
                Members = members;
                IsLoading = false;
            },
            unauthorized =>
            {
                LoadErrorMessage = "You are not authorized to view club members.";
                IsLoading = false;
            },
            error =>
            {
                LoadErrorMessage = "An unexpected error occurred while loading members.";
                IsLoading = false;
            });
    }

    private void ShowRemoveConfirmation(ClubMemberDto member)
    {
        ErrorMessage = null;
        ConfirmingRemoveMember = member;
    }

    private void CancelRemoveConfirmation()
    {
        ConfirmingRemoveMember = null;
        ErrorMessage = null;
    }

    private async Task ConfirmRemove()
    {
        if (ConfirmingRemoveMember is not null && !IsProcessing)
        {
            IsProcessing = true;
            ErrorMessage = null;

            var result = await calcioUsersService.RemoveClubMemberAsync(
                ClubId,
                ConfirmingRemoveMember.UserId,
                CancellationToken);

            result.Switch(
                success =>
                {
                    Members.Remove(ConfirmingRemoveMember);
                    ConfirmingRemoveMember = null;
                    IsProcessing = false;
                },
                notFound =>
                {
                    ErrorMessage = "The member could not be found.";
                    IsProcessing = false;
                },
                unauthorized =>
                {
                    ErrorMessage = "You are not authorized to remove this member.";
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
