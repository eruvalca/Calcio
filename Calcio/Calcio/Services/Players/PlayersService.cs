using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Players;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Services.BlobStorage;
using Calcio.Shared.Services.Players;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using OneOf;
using OneOf.Types;

using SkiaSharp;

using System.ComponentModel;

namespace Calcio.Services.Players;

/// <summary>
/// Internal record for caching blob paths without SAS URLs via HybridCache.
/// SAS URLs are generated on-demand to ensure they always have full validity period.
/// Marked as sealed and immutable for HybridCache instance reuse.
/// </summary>
[ImmutableObject(true)]
internal sealed record CachedPlayerPhotoPaths(
    long PlayerPhotoId,
    string OriginalBlobName,
    string? SmallBlobName,
    string? MediumBlobName,
    string? LargeBlobName);

public partial class PlayersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    IBlobStorageService blobStorageService,
    HybridCache cache,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PlayersService> logger) : AuthenticatedServiceBase(httpContextAccessor), IPlayersService
{
    private const string ContainerName = "player-photos";
    private const int SmallSize = 128;
    private const int MediumSize = 512;
    private const int LargeSize = 1024;
    private static readonly TimeSpan SasUrlExpiration = TimeSpan.FromHours(1);

    public async Task<ServiceResult<List<ClubPlayerDto>>> GetClubPlayersAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var players = await dbContext.Players
            .Where(p => p.ClubId == clubId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => p.ToClubPlayerDto())
            .ToListAsync(cancellationToken);

        LogPlayersRetrieved(logger, clubId, players.Count, CurrentUserId);
        return players;
    }

    public async Task<ServiceResult<PlayerCreatedDto>> CreatePlayerAsync(long clubId, CreatePlayerDto dto, CancellationToken cancellationToken)
    {
        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var player = new PlayerEntity
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            GraduationYear = dto.GraduationYear,
            Gender = dto.Gender,
            JerseyNumber = dto.JerseyNumber,
            TryoutNumber = dto.TryoutNumber,
            ClubId = clubId,
            CreatedById = CurrentUserId
        };

        dbContext.Players.Add(player);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogPlayerCreated(logger, player.PlayerId, clubId, CurrentUserId);

        return new PlayerCreatedDto(
            player.PlayerId,
            player.FirstName,
            player.LastName,
            player.FullName);
    }

    public async Task<ServiceResult<PlayerPhotoDto>> UploadPlayerPhotoAsync(
        long clubId,
        long playerId,
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Process image with SkiaSharp outside of transaction (CPU-bound work)
        var processedImages = await ProcessImageAsync(photoStream, cancellationToken);

        // Generate unique blob path
        var blobGuid = Guid.NewGuid().ToString("N");
        var basePath = $"players/{playerId}/{blobGuid}";

        // Upload all variants in parallel outside of transaction
        var uploadTasks = new List<Task<(string variant, Uri uri)>>();

        foreach (var (variant, imageData) in processedImages)
        {
            var blobName = $"{basePath}/{variant}.jpg";
            uploadTasks.Add(UploadVariantAsync(blobName, imageData, variant, cancellationToken));
        }

        var uploadResults = await Task.WhenAll(uploadTasks);
        var blobNames = uploadResults.ToDictionary(r => r.variant, r => $"{basePath}/{r.variant}.jpg");

        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Use execution strategy to wrap explicit transaction for retry support.
        // This is required when using NpgsqlRetryingExecutionStrategy.
        var strategy = dbContext.Database.CreateExecutionStrategy();

        string? oldBlobPrefix = null;
        PlayerPhotoEntity? photoEntity = null;

        var result = await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                // Verify player exists and belongs to club
                var player = await dbContext.Players
                    .Include(p => p.Photos)
                    .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.ClubId == clubId, ct);

                if (player is null)
                {
                    return ServiceProblem.NotFound("Player not found.");
                }

                // Track existing photo for cleanup after successful commit
                var existingPhoto = player.Photos.FirstOrDefault();

                if (existingPhoto is not null)
                {
                    oldBlobPrefix = $"players/{playerId}/{GetBlobGuidFromPath(existingPhoto.OriginalBlobName)}/";
                    dbContext.PlayerPhotos.Remove(existingPhoto);
                }

                // Create new photo entity
                photoEntity = new PlayerPhotoEntity
                {
                    OriginalBlobName = blobNames["original"],
                    SmallBlobName = blobNames.GetValueOrDefault("small"),
                    MediumBlobName = blobNames.GetValueOrDefault("medium"),
                    LargeBlobName = blobNames.GetValueOrDefault("large"),
                    ContentType = "image/jpeg",
                    PlayerId = playerId,
                    ClubId = clubId,
                    CreatedById = CurrentUserId
                };

                dbContext.PlayerPhotos.Add(photoEntity);

                // Update player's primary photo reference
                player.PrimaryPhotoBlobName = photoEntity.OriginalBlobName;

                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return (ServiceResult<PlayerPhotoDto>)GeneratePhotoDto(photoEntity);
            }
            catch
            {
                // Transaction will be rolled back automatically on dispose if not committed.
                // New blobs uploaded in this attempt become orphaned but can be cleaned up
                // by a background job or will be overwritten on next successful upload.
                throw;
            }
        }, cancellationToken);

        // Post-transaction cleanup and logging (only on success)
        if (result.IsSuccess)
        {
            // Delete old blobs only after successful commit to avoid orphaned records
            if (oldBlobPrefix is not null)
            {
                await blobStorageService.DeleteByPrefixAsync(ContainerName, oldBlobPrefix, cancellationToken);
                LogExistingPhotoDeleted(logger, playerId, CurrentUserId);
            }

            // Invalidate cache for this player's photo paths
            await cache.RemoveAsync($"player-photo-paths-{playerId}", cancellationToken);

            LogPhotoUploaded(logger, playerId, photoEntity!.PlayerPhotoId, CurrentUserId);
        }

        return result;
    }

    public async Task<ServiceResult<OneOf<PlayerPhotoDto, None>>> GetPlayerPhotoAsync(long clubId, long playerId, CancellationToken cancellationToken)
    {
        var cacheKey = $"player-photo-paths-{playerId}";

        // Cache only the blob paths, not the SAS URLs.
        // This ensures every request gets fresh SAS URLs with full validity period.
        var cachedPaths = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(ct);

                // Use IgnoreQueryFilters because:
                // 1. We explicitly filter by playerId AND clubId, which provides sufficient access control
                // 2. The DbContext created via factory inside this cache delegate may not have
                //    the correct CurrentUserIdForFilters set (HttpContext may not be available
                //    in the async context when HybridCache executes the factory delegate)
                // 3. The calling code should verify club membership before calling this method
                var photo = await dbContext.PlayerPhotos
                    .IgnoreQueryFilters()
                    .Where(p => p.PlayerId == playerId && p.ClubId == clubId)
                    .FirstOrDefaultAsync(ct);

                if (photo is null)
                {
                    return null;
                }

                return new CachedPlayerPhotoPaths(
                    photo.PlayerPhotoId,
                    photo.OriginalBlobName,
                    photo.SmallBlobName,
                    photo.MediumBlobName,
                    photo.LargeBlobName);
            },
            cancellationToken: cancellationToken);

        if (cachedPaths is null)
        {
            return (OneOf<PlayerPhotoDto, None>)new None();
        }

        // Generate fresh SAS URLs on every request
        return (OneOf<PlayerPhotoDto, None>)GeneratePhotoDtoFromPaths(cachedPaths);
    }

    private PlayerPhotoDto GeneratePhotoDto(PlayerPhotoEntity photo)
        => new(
            photo.PlayerPhotoId,
            blobStorageService.GetSasUrl(ContainerName, photo.OriginalBlobName, SasUrlExpiration).ToString(),
            photo.SmallBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.SmallBlobName, SasUrlExpiration).ToString() : null,
            photo.MediumBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.MediumBlobName, SasUrlExpiration).ToString() : null,
            photo.LargeBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.LargeBlobName, SasUrlExpiration).ToString() : null);

    private PlayerPhotoDto GeneratePhotoDtoFromPaths(CachedPlayerPhotoPaths paths)
        => new(
            paths.PlayerPhotoId,
            blobStorageService.GetSasUrl(ContainerName, paths.OriginalBlobName, SasUrlExpiration).ToString(),
            paths.SmallBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, paths.SmallBlobName, SasUrlExpiration).ToString() : null,
            paths.MediumBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, paths.MediumBlobName, SasUrlExpiration).ToString() : null,
            paths.LargeBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, paths.LargeBlobName, SasUrlExpiration).ToString() : null);

    private static async Task<Dictionary<string, byte[]>> ProcessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, byte[]>();

        // Read stream into memory for SkiaSharp
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var originalBitmap = SKBitmap.Decode(memoryStream)
            ?? throw new InvalidOperationException("Failed to decode image. The file may be corrupted or in an unsupported format.");

        // Auto-orient based on EXIF data (SkiaSharp handles this during decode in most cases)
        // Center-crop to square
        using var croppedBitmap = CropToSquare(originalBitmap);

        // All output variants are normalized to JPEG format regardless of input format.
        // This ensures consistent storage format and optimal file sizes for photos.
        results["original"] = EncodeToJpeg(croppedBitmap, 90);

        using (var largeBitmap = ResizeBitmap(croppedBitmap, LargeSize))
        {
            results["large"] = EncodeToJpeg(largeBitmap, 85);
        }

        using (var mediumBitmap = ResizeBitmap(croppedBitmap, MediumSize))
        {
            results["medium"] = EncodeToJpeg(mediumBitmap, 80);
        }

        using (var smallBitmap = ResizeBitmap(croppedBitmap, SmallSize))
        {
            results["small"] = EncodeToJpeg(smallBitmap, 75);
        }

        return results;
    }

    private static SKBitmap CropToSquare(SKBitmap source)
    {
        var size = Math.Min(source.Width, source.Height);
        var x = (source.Width - size) / 2;
        var y = (source.Height - size) / 2;

        var cropRect = new SKRectI(x, y, x + size, y + size);
        var croppedBitmap = new SKBitmap(size, size);

        using var canvas = new SKCanvas(croppedBitmap);
        canvas.DrawBitmap(source, cropRect, new SKRect(0, 0, size, size));

        return croppedBitmap;
    }

    private static SKBitmap ResizeBitmap(SKBitmap source, int maxSize)
    {
        if (source.Width <= maxSize && source.Height <= maxSize)
        {
            // Return a copy if already smaller than target
            var copy = new SKBitmap(source.Width, source.Height);
            using var copyCanvas = new SKCanvas(copy);
            copyCanvas.DrawBitmap(source, 0, 0);
            return copy;
        }

        var scale = (float)maxSize / Math.Max(source.Width, source.Height);
        var newWidth = (int)(source.Width * scale);
        var newHeight = (int)(source.Height * scale);

        var info = new SKImageInfo(newWidth, newHeight, source.ColorType, source.AlphaType);
        var resized = source.Resize(info, SKSamplingOptions.Default);

        // If resize fails (shouldn't with SKSamplingOptions.Default), return a copy
        // to maintain consistent ownership semantics (caller always owns the result)
        if (resized is null)
        {
            var fallbackCopy = new SKBitmap(source.Width, source.Height);
            using var fallbackCanvas = new SKCanvas(fallbackCopy);
            fallbackCanvas.DrawBitmap(source, 0, 0);
            return fallbackCopy;
        }

        return resized;
    }

    private static byte[] EncodeToJpeg(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    private async Task<(string variant, Uri uri)> UploadVariantAsync(string blobName, byte[] imageData, string variant, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(imageData);
        var uri = await blobStorageService.UploadAsync(ContainerName, blobName, stream, "image/jpeg", cancellationToken);
        return (variant, uri);
    }

    private static string GetBlobGuidFromPath(string blobPath)
    {
        // Path format: players/{playerId}/{guid}/variant.jpg
        var parts = blobPath.Split('/');
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved {PlayerCount} players for club {ClubId} by user {UserId}")]
    private static partial void LogPlayersRetrieved(ILogger logger, long clubId, int playerCount, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created player {PlayerId} for club {ClubId} by user {UserId}")]
    private static partial void LogPlayerCreated(ILogger logger, long playerId, long clubId, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded photo {PhotoId} for player {PlayerId} by user {UserId}")]
    private static partial void LogPhotoUploaded(ILogger logger, long playerId, long photoId, long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted existing photo for player {PlayerId} by user {UserId}")]
    private static partial void LogExistingPhotoDeleted(ILogger logger, long playerId, long userId);
}
