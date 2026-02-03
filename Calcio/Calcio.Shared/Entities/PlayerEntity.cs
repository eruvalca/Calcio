using Calcio.Shared.Entities.Base;
using Calcio.Shared.Enums;

namespace Calcio.Shared.Entities;

public class PlayerEntity : BaseEntity
{
    public long PlayerId { get; set; } = default;
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public required DateOnly DateOfBirth { get; set; }
    public string? PrimaryPhotoBlobName { get; set; }
    public Gender? Gender { get; set; }
    public int? JerseyNumber { get; set; }
    public int? TryoutNumber { get; set; }
    public required int GraduationYear { get; set; }

    public ICollection<NoteEntity> Notes { get; set; } = [];
    public ICollection<PlayerTagEntity> Tags { get; set; } = [];
    public ICollection<PlayerCampaignAssignmentEntity> CampaignAssignments { get; set; } = [];
    public ICollection<PlayerPhotoEntity> Photos { get; set; } = [];

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
