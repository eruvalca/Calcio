using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Validation;

namespace Calcio.Shared.DTOs.Teams;

public record CreateTeamDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [Required]
    [GraduationYear]
    int GraduationYear);
