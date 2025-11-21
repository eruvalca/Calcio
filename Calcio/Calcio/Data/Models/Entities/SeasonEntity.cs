using Calcio.Data.Models.Entities.Base;

namespace Calcio.Data.Models.Entities;

public class SeasonEntity : BaseEntity
{
    public long SeasonId { get; set; } = default!;
    public required string Name { get; set; }
    public required DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public ICollection<CampaignEntity> Campaigns { get; set; } = [];

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;

    public bool IsComplete => EndDate.HasValue;
}
