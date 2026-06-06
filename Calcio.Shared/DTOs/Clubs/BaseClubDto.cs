namespace Calcio.Shared.DTOs.Clubs;

/// <summary>
/// Represents the shared identifying and location information for a club.
/// </summary>
/// <param name="Id">The unique identifier of the club.</param>
/// <param name="Name">The display name of the club.</param>
/// <param name="City">The city where the club is based.</param>
/// <param name="State">The two-letter U.S. state abbreviation for the club location.</param>
public record BaseClubDto(
    long Id,
    string Name,
    string City,
    string State);
