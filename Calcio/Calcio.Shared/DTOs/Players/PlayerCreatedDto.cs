namespace Calcio.Shared.DTOs.Players;

public record PlayerCreatedDto(
    long PlayerId,
    string FirstName,
    string LastName,
    string FullName);
