using Calcio.Shared.Models.Entities.Base;

namespace Calcio.Shared.Models.Entities;

public class NoteEntity : BaseEntity
{
    public long NoteId { get; set; } = default!;
    public required string Content { get; set; }

    public required long PlayerId { get; set; }
    public PlayerEntity Player { get; set; } = null!;

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
