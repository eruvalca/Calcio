using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.Players;

public record PlayerImportResultDto(
    long ImportId,
    PlayerImportStatus Status,
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    string? ErrorMessage);
