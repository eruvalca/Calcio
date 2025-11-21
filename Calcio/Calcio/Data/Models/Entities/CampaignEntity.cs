using Calcio.Data.Models.Entities.Base;

namespace Calcio.Data.Models.Entities;

public class CampaignEntity : BaseEntity
{
    public long CampaignId { get; set; } = default!;
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly? EndDate { get; set; } = null;

    public ICollection<PlayerCampaignAssignmentEntity> PlayerAssignments { get; set; } = [];

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;

    public required long SeasonId { get; set; }
    public SeasonEntity Season { get; set; } = null!;

    public bool IsComplete => EndDate.HasValue;
}
