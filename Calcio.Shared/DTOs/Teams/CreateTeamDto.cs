using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Teams;

/// <summary>
/// Represents the input payload used to create a team.
/// </summary>
/// <param name="Name">The display name of the team.</param>
/// <param name="GraduationYear">The graduation year group the team represents.</param>
public record CreateTeamDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [Required]
    [GraduationYear]
    int GraduationYear);
