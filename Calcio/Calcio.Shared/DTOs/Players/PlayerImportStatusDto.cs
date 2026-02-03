using Calcio.Shared.Enums;

namespace Calcio.Shared.DTOs.Players;

public record PlayerImportStatusDto(
    long ImportId,
    string FileName,
    PlayerImportStatus Status,
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    List<PlayerImportRowDto> Rows);
