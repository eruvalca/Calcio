using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.CalcioUsers.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class ClubMembersGrid(ICalcioUsersService calcioUsersService)
{
    [Parameter]
    public required long ClubId { get; set; }

    [Parameter]
    public List<ClubMemberDto> Members { get; set; } = [];

    private string SearchTerm { get; set; } = string.Empty;

    private ClubMemberDto? ConfirmingRemoveMember { get; set; }

    private bool IsProcessing { get; set; }

    private string? ErrorMessage { get; set; }

    private IEnumerable<ClubMemberDto> FilteredMembers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Members
            : Members.Where(member => member.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                || member.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

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
                problem =>
                {
                    ErrorMessage = problem.Kind switch
                    {
                        ServiceProblemKind.NotFound => "The member could not be found.",
                        ServiceProblemKind.Forbidden => "You are not authorized to remove this member.",
                        _ => problem.Detail ?? "An unexpected error occurred. Please try again."
                    };
                    IsProcessing = false;
                });
        }
    }
}
