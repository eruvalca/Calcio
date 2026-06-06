using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.Players;

/// <summary>
/// Represents a player as returned in club player listings.
/// </summary>
/// <param name="PlayerId">The unique identifier of the player.</param>
/// <param name="FirstName">The player's first name.</param>
/// <param name="LastName">The player's last name.</param>
/// <param name="FullName">The player's full display name.</param>
/// <param name="DateOfBirth">The player's date of birth.</param>
/// <param name="Gender">The player's reported gender, when provided.</param>
/// <param name="JerseyNumber">The player's jersey number, when assigned.</param>
/// <param name="TryoutNumber">The player's tryout number, when assigned.</param>
public record ClubPlayerDto(
    long PlayerId,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    Gender? Gender,
    int? JerseyNumber,
    int? TryoutNumber);
