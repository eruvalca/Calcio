namespace Calcio.Shared.DTOs.Seasons;

public record SeasonDto(
    long SeasonId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsComplete);
