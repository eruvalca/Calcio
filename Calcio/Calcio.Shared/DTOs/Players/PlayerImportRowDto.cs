namespace Calcio.Shared.DTOs.Players;

public record PlayerImportRowDto(
    int RowNumber,
    bool IsSuccess,
    string? ErrorMessage,
    long? CreatedPlayerId,
    string? PlayerFullName);
