namespace Calcio.Shared.DTOs.Teams;

/// <summary>
/// Represents a team belonging to a club.
/// </summary>
/// <param name="TeamId">The unique identifier of the team.</param>
/// <param name="Name">The team name.</param>
/// <param name="GraduationYear">The graduation year group the team represents.</param>
public record TeamDto(
    long TeamId,
    string Name,
    int GraduationYear);
