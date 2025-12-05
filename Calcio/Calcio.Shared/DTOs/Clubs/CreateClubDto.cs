using System.ComponentModel.DataAnnotations;

namespace Calcio.Shared.DTOs.Clubs;

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
