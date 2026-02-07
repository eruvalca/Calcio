using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;
using Calcio.Shared.Services.Clubs;
using Calcio.UI.Services.Clubs;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

public partial class ClubMembershipPanel(
    IClubsService clubsService,
    IAccountService accountService,
    UserClubStateService userClubStateService)
{
    private static readonly string[] UsStateAbbreviations =
    [
        "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT","VA","WA","WV","WI","WY"
    ];

    [Parameter]
    public List<BaseClubDto> AllClubs { get; set; } = [];

    [Parameter]
    public ClubJoinRequestDto? CurrentJoinRequest { get; set; }

    private InputModel Input { get; } = new();
    private BaseClubDto? CreatedClub { get; set; }
    private string? StatusMessage { get; set; }
    private string StatusMessageClass { get; set; } = "success";
    private bool IsSubmitting { get; set; }

    private async Task CreateClubAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;
        StatusMessage = null;
        try
        {
            var createDto = new CreateClubDto(Input.Name, Input.City, Input.State);
            var result = await clubsService.CreateClubAsync(createDto, CancellationToken);

            result.Switch(
                club =>
                {
                    CreatedClub = new BaseClubDto(club.ClubId, club.Name, Input.City, Input.State);
                    userClubStateService.SetUserClubs([CreatedClub]);
                    StatusMessageClass = "success";
                    StatusMessage = $"Club '{club.Name}' created.";
                },
                problem =>
                {
                    StatusMessageClass = "danger";
                    StatusMessage = problem.Kind switch
                    {
                        ServiceProblemKind.Conflict => "That club already exists.",
                        ServiceProblemKind.Forbidden => "You do not have permission to create a club.",
                        ServiceProblemKind.BadRequest => "Invalid club details.",
                        _ => "An unexpected error occurred while creating the club."
                    };
                });

            if (result.IsSuccess)
            {
                await accountService.RefreshSignInAsync(CancellationToken);
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "Club Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(AL|AK|AZ|AR|CA|CO|CT|DE|FL|GA|HI|ID|IL|IN|IA|KS|KY|LA|ME|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|OH|OK|OR|PA|RI|SC|SD|TN|TX|UT|VT|VA|WA|WV|WI|WY)$", ErrorMessage = "Invalid state abbreviation.")]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;
    }
}
