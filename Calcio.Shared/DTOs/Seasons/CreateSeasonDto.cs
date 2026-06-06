using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Seasons;

/// <summary>
/// Represents the input payload used to create a season for a club.
/// </summary>
/// <param name="Name">The display name of the season.</param>
/// <param name="StartDate">The date the season begins.</param>
/// <param name="EndDate">The optional date the season ends.</param>
public record CreateSeasonDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [Required]
    [DateNotBeforeToday]
    DateOnly StartDate,

    [DateAfterToday]
    [DateAfter(nameof(StartDate))]
    DateOnly? EndDate = null);
