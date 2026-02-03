using Calcio.Shared.Entities.Base;
using Calcio.Shared.Enums;

namespace Calcio.Shared.Entities;

public class PlayerImportEntity : BaseEntity
{
    public long ImportId { get; set; } = default;
    public required string FileName { get; set; }
    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
    
    public PlayerImportStatus Status { get; set; } = PlayerImportStatus.Pending;
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    
    public ICollection<PlayerImportRowEntity> Rows { get; set; } = [];
}
