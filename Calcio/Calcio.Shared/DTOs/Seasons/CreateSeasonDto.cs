using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Seasons;

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
