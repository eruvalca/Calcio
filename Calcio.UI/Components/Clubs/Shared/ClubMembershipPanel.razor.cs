using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Account;
using Calcio.Shared.Services.Clubs;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Clubs.Shared;

/// <summary>
/// Displays club membership status and provides club-creation functionality.
/// </summary>
/// <param name="clubsService">The service used to create clubs.</param>
/// <param name="accountService">The service used to refresh sign-in state after club creation.</param>
public partial class ClubMembershipPanel(
    IClubsService clubsService,
    IAccountService accountService)
{
    /// <summary>
    /// Provides the set of valid U.S. state abbreviations for club creation.
    /// </summary>
    private static readonly string[] UsStateAbbreviations =
    [
        "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT","VA","WA","WV","WI","WY"
    ];

    /// <summary>
    /// Gets or sets all clubs that can be displayed in membership views.
    /// </summary>
    [Parameter]
    public List<BaseClubDto> AllClubs { get; set; } = [];

    /// <summary>
    /// Gets or sets the current join request for the signed-in user.
    /// </summary>
    [Parameter]
    public ClubJoinRequestDto? CurrentJoinRequest { get; set; }

    /// <summary>
    /// Gets the input model bound to the create-club form.
    /// </summary>
    private InputModel Input { get; } = new();

    /// <summary>
    /// Gets or sets the club created by the most recent successful submission.
    /// </summary>
    private BaseClubDto? CreatedClub { get; set; }

    /// <summary>
    /// Gets or sets the status message displayed after submission.
    /// </summary>
    private string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the Bootstrap status class suffix used for feedback styling.
    /// </summary>
    private string StatusMessageClass { get; set; } = "success";

    /// <summary>
    /// Gets or sets a value indicating whether club creation is currently in progress.
    /// </summary>
    private bool IsSubmitting { get; set; }

    /// <summary>
    /// Creates a club from the current input model and refreshes authentication state on success.
    /// </summary>
    /// <returns>A task that completes when submission handling finishes.</returns>
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

    /// <summary>
    /// Represents form input used to create a new club.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the club name.
        /// </summary>
        [Required]
        [Display(Name = "Club Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city where the club is located.
        /// </summary>
        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the U.S. state abbreviation for the club location.
        /// </summary>
        [Required]
        [RegularExpression("^(AL|AK|AZ|AR|CA|CO|CT|DE|FL|GA|HI|ID|IL|IN|IA|KS|KY|LA|ME|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|OH|OK|OR|PA|RI|SC|SD|TN|TX|UT|VT|VA|WA|WV|WI|WY)$", ErrorMessage = "Invalid state abbreviation.")]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;
    }
}
