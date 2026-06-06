using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.DTOs.Clubs;

/// <summary>
/// Represents the input required to create a new club.
/// </summary>
/// <param name="Name">The club name entered by the user.</param>
/// <param name="City">The city where the club is located.</param>
/// <param name="State">The two-letter U.S. state abbreviation for the club location.</param>
public record CreateClubDto(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Name,

    [Required]
    [StringLength(100, MinimumLength = 2)]
    string City,

    [Required]
    [RegularExpression("^(AL|AK|AZ|AR|CA|CO|CT|DE|FL|GA|HI|ID|IL|IN|IA|KS|KY|LA|ME|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|OH|OK|OR|PA|RI|SC|SD|TN|TX|UT|VT|VA|WA|WV|WI|WY)$", ErrorMessage = "Invalid US state abbreviation.")]
    string State);
