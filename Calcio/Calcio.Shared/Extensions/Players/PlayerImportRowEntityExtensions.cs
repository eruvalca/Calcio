using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Entities;

namespace Calcio.Shared.Extensions.Players;

public static class PlayerImportRowEntityExtensions
{
    extension(PlayerImportRowEntity row)
    {
        public PlayerImportRowDto ToPlayerImportRowDto()
            => new(
                RowNumber: row.RowNumber,
                IsSuccess: row.IsSuccess,
                ErrorMessage: row.ErrorMessage,
                CreatedPlayerId: row.CreatedPlayerId,
                PlayerFullName: row.CreatedPlayer?.FullName);
    }
}
