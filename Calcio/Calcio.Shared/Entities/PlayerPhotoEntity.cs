using Calcio.Shared.Entities.Base;

namespace Calcio.Shared.Entities;

public class PlayerPhotoEntity : BaseEntity
{
    public long PlayerPhotoId { get; set; } = default!;
    public required string OriginalBlobName { get; set; }
    public string? SmallBlobName { get; set; }
    public string? MediumBlobName { get; set; }
    public string? LargeBlobName { get; set; }
    public string? ContentType { get; set; }

    public required long PlayerId { get; set; }
    public PlayerEntity Player { get; set; } = null!;

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;
}
