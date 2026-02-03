using Calcio.Shared.Entities.Base;

namespace Calcio.Shared.Entities;

public class PlayerImportRowEntity : BaseEntity
{
    public long RowId { get; set; } = default;
    public required long ImportId { get; set; }
    public PlayerImportEntity Import { get; set; } = null!;
    
    public required int RowNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    public long? CreatedPlayerId { get; set; }
    public PlayerEntity? CreatedPlayer { get; set; }
    
    // Store original row data for audit
    public required string RawData { get; set; }
}
