namespace Calcio.Shared.DTOs.CalcioUsers;

public record ClubMemberDto(
    long UserId,
    string FullName,
    string Email,
    bool IsClubAdmin);
