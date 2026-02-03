using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Entities;

namespace Calcio.Shared.Extensions.Players;

public static class PlayerImportEntityExtensions
{
    extension(PlayerImportEntity import)
    {
        public PlayerImportStatusDto ToPlayerImportStatusDto()
            => new(
                ImportId: import.ImportId,
                FileName: import.FileName,
                Status: import.Status,
                TotalRows: import.TotalRows,
                SuccessfulRows: import.SuccessfulRows,
                FailedRows: import.FailedRows,
                ErrorMessage: import.ErrorMessage,
                CreatedAt: import.CreatedAt,
                CompletedAt: import.CompletedAt,
                Rows: import.Rows.Select(r => r.ToPlayerImportRowDto()).ToList());

        public PlayerImportResultDto ToPlayerImportResultDto()
            => new(
                ImportId: import.ImportId,
                Status: import.Status,
                TotalRows: import.TotalRows,
                SuccessfulRows: import.SuccessfulRows,
                FailedRows: import.FailedRows,
                ErrorMessage: import.ErrorMessage);
    }
}
