namespace Calcio.Shared.DTOs.Players;

/// <summary>
/// Represents the data returned after a player is created.
/// </summary>
/// <param name="PlayerId">The unique identifier assigned to the new player.</param>
/// <param name="FirstName">The player's first name.</param>
/// <param name="LastName">The player's last name.</param>
/// <param name="FullName">The player's full display name.</param>
public record PlayerCreatedDto(
    long PlayerId,
    string FirstName,
    string LastName,
    string FullName);
