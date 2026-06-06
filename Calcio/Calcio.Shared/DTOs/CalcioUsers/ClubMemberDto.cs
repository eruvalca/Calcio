namespace Calcio.Shared.DTOs.CalcioUsers;

/// <summary>
/// Represents a user who belongs to a club, including their role within that club.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="FullName">The display name of the user.</param>
/// <param name="Email">The email address associated with the user account.</param>
/// <param name="IsClubAdmin">A value indicating whether the user is an administrator for the club.</param>
public record ClubMemberDto(
    long UserId,
    string FullName,
    string Email,
    bool IsClubAdmin);
