using System.ComponentModel;

using Calcio.Data.Contexts;
using Calcio.Shared.Caching;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Entities;
using Calcio.Shared.Extensions.CalcioUsers;
using Calcio.Shared.Results;
using Calcio.Shared.Security;
using Calcio.Shared.Services.BlobStorage;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using OneOf;
using OneOf.Types;

using SkiaSharp;

namespace Calcio.Services.CalcioUsers;

/// <summary>
/// Internal record for caching blob paths without SAS URLs via HybridCache.
/// SAS URLs are generated on-demand to ensure they always have full validity period.
/// Marked as sealed and immutable for HybridCache instance reuse.
/// </summary>
[ImmutableObject(true)]
internal sealed record CachedUserPhotoPaths(
    long CalcioUserPhotoId,
    string OriginalBlobName,
    string? SmallBlobName,
    string? MediumBlobName,
    string? LargeBlobName);

public partial class CalcioUsersService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IUserClubsCacheService userClubsCacheService,
    IBlobStorageService blobStorageService,
    HybridCache cache,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CalcioUsersService> logger) : AuthenticatedServiceBase(httpContextAccessor), ICalcioUsersService
{
    private const string ContainerName = "user-photos";
    private const int SmallSize = 128;
    private const int MediumSize = 512;
    private const int LargeSize = 1024;
    private static readonly TimeSpan SasUrlExpiration = TimeSpan.FromHours(1);
    public async Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var users = await dbContext.Users
            .Where(u => u.ClubId == clubId && u.Id != CurrentUserId)
            .ToListAsync(cancellationToken);

        var members = new List<ClubMemberDto>();
        foreach (var user in users)
        {
            var isClubAdmin = await userManager.IsInRoleAsync(user, Roles.ClubAdmin);
            members.Add(user.ToClubMemberDto(isClubAdmin));
        }

        return members.OrderByDescending(m => m.IsClubAdmin).ThenBy(m => m.FullName).ToList();
    }

    public async Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken)
    {
        // Club membership is validated by ClubMembershipFilter before this service is called.
        if (userId == CurrentUserId)
        {
            return ServiceProblem.Forbidden("You cannot remove yourself from the club.");
        }

        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var userToRemove = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.ClubId == clubId, cancellationToken);

        if (userToRemove is null)
        {
            return ServiceProblem.NotFound();
        }

        userToRemove.ClubId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var userForRoleRemoval = await userManager.FindByIdAsync(userId.ToString());
        if (userForRoleRemoval is not null)
        {
            var removeRoleResult = await userManager.RemoveFromRoleAsync(userForRoleRemoval, Roles.StandardUser);
            if (!removeRoleResult.Succeeded)
            {
                var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                LogRoleRemovalFailed(logger, userId, Roles.StandardUser, errors);
            }
        }

        LogMemberRemoved(logger, clubId, userId, CurrentUserId);

        // Invalidate the removed user's clubs cache since they no longer belong to this club
        await userClubsCacheService.InvalidateUserClubsCacheAsync(userId, cancellationToken);

        return new Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Member {UserId} removed from club {ClubId} by user {RemovingUserId}")]
    private static partial void LogMemberRemoved(ILogger logger, long clubId, long userId, long removingUserId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove {RoleName} role from user {UserId}: {Errors}")]
    private static partial void LogRoleRemovalFailed(ILogger logger, long userId, string roleName, string errors);

    public async Task<ServiceResult<CalcioUserPhotoDto>> UploadAccountPhotoAsync(
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Process image with SkiaSharp outside of transaction (CPU-bound work)
        var processedImages = await ProcessImageAsync(photoStream, cancellationToken);

        // Generate unique blob path
        var blobGuid = Guid.NewGuid().ToString("N");
        var basePath = $"users/{CurrentUserId}/{blobGuid}";

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
        var strategy = dbContext.Database.CreateExecutionStrategy();

        string? oldBlobPrefix = null;
        CalcioUserPhotoEntity? photoEntity = null;

        var result = await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                // Get current user with photos
                var user = await dbContext.Users
                    .Include(u => u.Photos)
                    .FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);

                if (user is null)
                {
                    return ServiceProblem.NotFound("User not found.");
                }

                // Track existing photo for cleanup after successful commit
                var existingPhoto = user.Photos.FirstOrDefault();

                if (existingPhoto is not null)
                {
                    oldBlobPrefix = $"users/{CurrentUserId}/{GetBlobGuidFromPath(existingPhoto.OriginalBlobName)}/";
                    dbContext.CalcioUserPhotos.Remove(existingPhoto);
                }

                // Create new photo entity
                photoEntity = new CalcioUserPhotoEntity
                {
                    OriginalBlobName = blobNames["original"],
                    SmallBlobName = blobNames.GetValueOrDefault("small"),
                    MediumBlobName = blobNames.GetValueOrDefault("medium"),
                    LargeBlobName = blobNames.GetValueOrDefault("large"),
                    ContentType = "image/jpeg",
                    CalcioUserId = CurrentUserId,
                    CreatedById = CurrentUserId
                };

                await dbContext.CalcioUserPhotos.AddAsync(photoEntity, ct);
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return (ServiceResult<CalcioUserPhotoDto>)GeneratePhotoDto(photoEntity);
            }
            catch
            {
                throw;
            }
        }, cancellationToken);

        // Post-transaction cleanup and logging (only on success)
        if (result.IsSuccess)
        {
            // Delete old blobs only after successful commit
            if (oldBlobPrefix is not null)
            {
                await blobStorageService.DeleteByPrefixAsync(ContainerName, oldBlobPrefix, cancellationToken);
                LogExistingPhotoDeleted(logger, CurrentUserId);
            }

            // Invalidate cache for this user's photo paths
            await cache.RemoveAsync(CacheDefaults.CalcioUsers.GetPhotoPathsKey(CurrentUserId), cancellationToken);

            LogPhotoUploaded(logger, CurrentUserId, photoEntity!.CalcioUserPhotoId);
        }

        return result;
    }

    public async Task<ServiceResult<OneOf<CalcioUserPhotoDto, None>>> GetAccountPhotoAsync(CancellationToken cancellationToken)
    {
        // Capture CurrentUserId before entering cache callback to avoid HttpContext access issues
        // when the callback executes asynchronously or on a different context (e.g., SSR prerender).
        var userId = CurrentUserId;
        var cacheKey = CacheDefaults.CalcioUsers.GetPhotoPathsKey(userId);

        // Cache only the blob paths, not the SAS URLs.
        var cachedPaths = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
                {
                    await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(ct);

                    var photo = await dbContext.CalcioUserPhotos
                        .Where(p => p.CalcioUserId == userId)
                        .FirstOrDefaultAsync(ct);

                    if (photo is null)
                    {
                        return null;
                    }

                    return new CachedUserPhotoPaths(
                        photo.CalcioUserPhotoId,
                        photo.OriginalBlobName,
                        photo.SmallBlobName,
                        photo.MediumBlobName,
                        photo.LargeBlobName);
                },
            options: CacheDefaults.CalcioUsers.EntryOptions,
            cancellationToken: cancellationToken);

        if (cachedPaths is null)
        {
            return (OneOf<CalcioUserPhotoDto, None>)new None();
        }

        // Generate fresh SAS URLs on every request
        return (OneOf<CalcioUserPhotoDto, None>)GeneratePhotoDtoFromPaths(cachedPaths);
    }

    public async Task<ServiceResult<bool>> HasAccountPhotoAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var hasPhoto = await dbContext.CalcioUserPhotos
            .AnyAsync(p => p.CalcioUserId == CurrentUserId, cancellationToken);

        return hasPhoto;
    }

    private CalcioUserPhotoDto GeneratePhotoDto(CalcioUserPhotoEntity photo)
        => new(
            photo.CalcioUserPhotoId,
            blobStorageService.GetSasUrl(ContainerName, photo.OriginalBlobName, SasUrlExpiration).ToString(),
            photo.SmallBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.SmallBlobName, SasUrlExpiration).ToString() : null,
            photo.MediumBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.MediumBlobName, SasUrlExpiration).ToString() : null,
            photo.LargeBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.LargeBlobName, SasUrlExpiration).ToString() : null);

    private CalcioUserPhotoDto GeneratePhotoDtoFromPaths(CachedUserPhotoPaths paths)
        => new(
            paths.CalcioUserPhotoId,
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

        // Center-crop to square
        using var croppedBitmap = CropToSquare(originalBitmap);

        // All output variants are normalized to JPEG format
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
        // Path format: users/{userId}/{guid}/variant.jpg
        var parts = blobPath.Split('/');
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded photo {PhotoId} for user {UserId}")]
    private static partial void LogPhotoUploaded(ILogger logger, long userId, long photoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted existing photo for user {UserId}")]
    private static partial void LogExistingPhotoDeleted(ILogger logger, long userId);
}
