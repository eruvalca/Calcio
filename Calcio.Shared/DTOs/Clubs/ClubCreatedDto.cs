namespace Calcio.Shared.DTOs.Clubs;

/// <summary>
/// Represents the result of creating a club.
/// </summary>
/// <param name="ClubId">The unique identifier assigned to the newly created club.</param>
/// <param name="Name">The name of the newly created club.</param>
public record ClubCreatedDto(
    long ClubId,
    string Name);
