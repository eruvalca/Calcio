using Calcio.Shared.Entities.Base;

namespace Calcio.Shared.Entities;

public class PlayerCampaignAssignmentEntity : BaseEntity
{
    public long PlayerCampaignAssignmentId { get; set; } = default;

    public required long PlayerId { get; set; }
    public PlayerEntity Player { get; set; } = null!;

    public required long CampaignId { get; set; }
    public CampaignEntity Campaign { get; set; } = null!;

    public long? TeamId { get; set; } = null;
    public TeamEntity? Team { get; set; } = null;

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
