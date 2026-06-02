using Calcio.Entities.Base;

namespace Calcio.Entities;

public class TeamEntity : BaseEntity
{
    public long TeamId { get; set; } = default;
    public required string Name { get; set; }
    public required int GraduationYear { get; set; }

    public ICollection<PlayerCampaignAssignmentEntity> PlayerAssignments { get; set; } = [];

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
