namespace Calcio.Shared.DTOs.Players;

/// <summary>
/// Contains SAS URLs for all photo variants.
/// SAS URLs are generated on-demand and are not cached.
/// </summary>
public sealed record PlayerPhotoDto(
    long PlayerPhotoId,
    string OriginalUrl,
    string? SmallUrl,
    string? MediumUrl,
    string? LargeUrl);
