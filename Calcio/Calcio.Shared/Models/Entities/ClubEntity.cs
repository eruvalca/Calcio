using Calcio.Shared.Models.Entities.Base;

namespace Calcio.Shared.Models.Entities;

public class ClubEntity : BaseEntity
{
    public long ClubId { get; set; } = default!;
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }

    public ICollection<CalcioUserEntity> CalcioUsers { get; set; } = [];
    public ICollection<CampaignEntity> Campaigns { get; set; } = [];
    public ICollection<SeasonEntity> Seasons { get; set; } = [];
    public ICollection<TeamEntity> Teams { get; set; } = [];
    public ICollection<PlayerEntity> Players { get; set; } = [];
    public ICollection<PlayerTagEntity> PlayerTags { get; set; } = [];
    public ICollection<ClubJoinRequestEntity> JoinRequests { get; set; } = [];
}
