using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.Players;

public record ClubPlayerDto(
    long PlayerId,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    Gender? Gender,
    int? JerseyNumber,
    int? TryoutNumber);
