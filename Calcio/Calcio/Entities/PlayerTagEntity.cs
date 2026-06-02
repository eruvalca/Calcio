using Calcio.Entities.Base;

namespace Calcio.Entities;

public class PlayerTagEntity : BaseEntity
{
    public long PlayerTagId { get; set; } = default;
    public required string Name { get; set; }
    public required string Color { get; set; }

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
