namespace Calcio.Shared.DTOs.CalcioUsers;

/// <summary>
/// Contains SAS URLs for all photo variants.
/// SAS URLs are generated on-demand and are not cached.
/// </summary>
public sealed record CalcioUserPhotoDto(
    long CalcioUserPhotoId,
    string OriginalUrl,
    string? SmallUrl,
    string? MediumUrl,
    string? LargeUrl);
