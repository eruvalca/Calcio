using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Enums;
using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Players;

public record CreatePlayerDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string FirstName,

    [Required]
    [StringLength(100, MinimumLength = 1)]
    string LastName,

    [Required]
    DateOnly DateOfBirth,

    [Required]
    [GraduationYear]
    int GraduationYear,

    Gender? Gender = null,

    [Range(0, 999)]
    int? JerseyNumber = null,

    [Range(0, 9999)]
    int? TryoutNumber = null);
