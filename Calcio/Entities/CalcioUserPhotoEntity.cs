using Calcio.Entities.Base;

namespace Calcio.Entities;

/// <summary>
/// Represents the Calcio User Photo Entity persisted in the database.
/// </summary>
public class CalcioUserPhotoEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets the Calcio User Photo Id.
    /// </summary>
    public long CalcioUserPhotoId { get; set; } = default;
    /// <summary>
    /// Gets or sets the Original Blob Name.
    /// </summary>
    public required string OriginalBlobName { get; set; }
    /// <summary>
    /// Gets or sets the Small Blob Name.
    /// </summary>
    public string? SmallBlobName { get; set; }
    /// <summary>
    /// Gets or sets the Medium Blob Name.
    /// </summary>
    public string? MediumBlobName { get; set; }
    /// <summary>
    /// Gets or sets the Large Blob Name.
    /// </summary>
    public string? LargeBlobName { get; set; }
    /// <summary>
    /// Gets or sets the Content Type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the Calcio User Id.
    /// </summary>
    public required long CalcioUserId { get; set; }
    /// <summary>
    /// Gets or sets the Calcio User.
    /// </summary>
    public CalcioUserEntity CalcioUser { get; set; } = null!;
}
