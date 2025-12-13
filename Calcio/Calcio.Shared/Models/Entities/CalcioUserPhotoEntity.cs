namespace Calcio.Shared.Models.Entities;

public class CalcioUserPhotoEntity : BaseEntity
{
    public long CalcioUserPhotoId { get; set; } = default!;
    public required string OriginalBlobName { get; set; }
    public string? SmallBlobName { get; set; }
    public string? MediumBlobName { get; set; }
    public string? LargeBlobName { get; set; }
    public string? ContentType { get; set; }

    public required long CalcioUserId { get; set; }
    public CalcioUserEntity CalcioUser { get; set; } = null!;
}
