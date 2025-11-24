using Calcio.Shared.Models.Entities.Base;

namespace Calcio.Shared.Models.Entities;

public class TeamEntity : BaseEntity
{
    public long TeamId { get; set; } = default!;
    public required string Name { get; set; }
    public int? BirthYear { get; set; }

    public ICollection<PlayerCampaignAssignmentEntity> PlayerAssignments { get; set; } = [];

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
