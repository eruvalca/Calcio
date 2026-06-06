using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Enums;
using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Players;

/// <summary>
/// Represents the input payload used to create a new player.
/// </summary>
/// <param name="FirstName">The player's first name.</param>
/// <param name="LastName">The player's last name.</param>
/// <param name="DateOfBirth">The player's date of birth.</param>
/// <param name="GraduationYear">The expected high school graduation year for the player.</param>
/// <param name="Gender">The player's reported gender, when provided.</param>
/// <param name="JerseyNumber">The requested jersey number, when provided.</param>
/// <param name="TryoutNumber">The tryout number used during evaluations, when provided.</param>
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
