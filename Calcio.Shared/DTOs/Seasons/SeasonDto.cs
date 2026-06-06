namespace Calcio.Shared.DTOs.Seasons;

/// <summary>
/// Represents a season associated with a club.
/// </summary>
/// <param name="SeasonId">The unique identifier of the season.</param>
/// <param name="Name">The season name.</param>
/// <param name="StartDate">The date the season starts.</param>
/// <param name="EndDate">The date the season ends, when set.</param>
/// <param name="IsComplete">A value indicating whether the season has been completed.</param>
public record SeasonDto(
    long SeasonId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsComplete);
