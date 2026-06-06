using System.ComponentModel.DataAnnotations;

using Calcio.Shared.DTOs.Seasons;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Validation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Seasons.Shared;

[Authorize(Roles = Roles.ClubAdmin)]
/// <summary>
/// Displays seasons for a club, supports filtering, and allows new season creation.
/// </summary>
/// <param name="seasonService">The service used to create seasons.</param>
/// <param name="navigationManager">The navigation manager used to refresh after creation.</param>
public partial class SeasonsGrid(
    ISeasonsService seasonService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Gets or sets the club identifier associated with the seasons.
    /// </summary>
    [Parameter]
    public long ClubId { get; set; }

    /// <summary>
    /// Gets or sets the seasons displayed in the grid.
    /// </summary>
    [Parameter]
    public List<SeasonDto> Seasons { get; set; } = [];

    /// <summary>
    /// Gets or sets the current search term used to filter seasons.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets seasons matching the current search term.
    /// </summary>
    private IEnumerable<SeasonDto> FilteredSeasons
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Seasons
            : Seasons.Where(season => season.Name.ContainsIgnoreCase(SearchTerm)
                || season.StartDate.ToString().Contains(SearchTerm)
                || (season.EndDate?.ToString().Contains(SearchTerm) ?? false));

    /// <summary>
    /// Gets or sets a value indicating whether the create-season form is visible.
    /// </summary>
    private bool ShowCreateForm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a season is currently being created.
    /// </summary>
    private bool IsCreating { get; set; }

    /// <summary>
    /// Gets or sets the create-season error message.
    /// </summary>
    private string? CreateErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the create-season input model.
    /// </summary>
    private CreateSeasonInputModel CreateInput { get; set; } = new();

    /// <summary>
    /// Gets today's date in local time.
    /// </summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Gets tomorrow's date relative to <see cref="Today"/>.
    /// </summary>
    private static DateOnly Tomorrow => Today.AddDays(1);

    /// <summary>
    /// Determines whether the specified season is active today.
    /// </summary>
    /// <param name="season">The season to evaluate.</param>
    /// <returns><see langword="true"/> when the season is active; otherwise, <see langword="false"/>.</returns>
    private static bool IsSeasonActive(SeasonDto season)
    {
        var today = Today;
        var isWithinStartDate = today >= season.StartDate;
        var isWithinEndDate = season.EndDate is null || today <= season.EndDate;
        return isWithinStartDate && isWithinEndDate;
    }

    /// <summary>
    /// Toggles visibility of the create-season form.
    /// </summary>
    private void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        CreateErrorMessage = null;

        if (ShowCreateForm)
        {
            CreateInput = new CreateSeasonInputModel();
        }
    }

    /// <summary>
    /// Creates a season using the current form input.
    /// </summary>
    /// <returns>A task that completes when create handling finishes.</returns>
    private async Task HandleCreateSeason()
    {
        if (IsCreating)
        {
            return;
        }

        IsCreating = true;
        CreateErrorMessage = null;

        var dto = new CreateSeasonDto(CreateInput.Name, CreateInput.StartDate, CreateInput.EndDate);
        var result = await seasonService.CreateSeasonAsync(ClubId, dto, CancellationToken);

        result.Switch(
            success =>
            {
                ShowCreateForm = false;
                CreateInput = new CreateSeasonInputModel();
                IsCreating = false;
                navigationManager.Refresh();
            },
            problem =>
            {
                CreateErrorMessage = problem.Kind switch
                {
                    ServiceProblemKind.Forbidden => "You are not authorized to create seasons.",
                    ServiceProblemKind.Conflict => "A season with this name already exists.",
                    _ => problem.Detail ?? "An unexpected error occurred while creating the season."
                };
                IsCreating = false;
            });
    }

    /// <summary>
    /// Represents form input used to create a season.
    /// </summary>
    private sealed class CreateSeasonInputModel
    {
        /// <summary>
        /// Gets today's date in local time.
        /// </summary>
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        /// <summary>
        /// Gets or sets the season name.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [Display(Name = "Season Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the season start date, clamped to today or later.
        /// </summary>
        [Required]
        [DateNotBeforeToday]
        [Display(Name = "Start Date")]
        public DateOnly StartDate
        {
            get;
            set => field = value < Today ? Today : value;
        } = Today;

        /// <summary>
        /// Gets or sets the season end date, constrained to at least one day after start date.
        /// </summary>
        [DateAfterToday]
        [DateAfter(nameof(StartDate))]
        [Display(Name = "End Date")]
        public DateOnly? EndDate
        {
            get;
            set
            {
                if (value is null)
                {
                    field = null;
                }
                else
                {
                    var minEndDate = StartDate.AddDays(1);
                    field = value < minEndDate ? minEndDate : value;
                }
            }
        }
    }
}
