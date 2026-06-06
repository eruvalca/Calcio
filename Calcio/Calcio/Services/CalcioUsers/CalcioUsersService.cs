using System.ComponentModel;

using Calcio.Data.Contexts;
using Calcio.Shared.Caching;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Entities;
using Calcio.Extensions.CalcioUsers;
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
/// <summary>
/// Represents the cached user photo paths record.
/// </summary>
internal sealed record CachedUserPhotoPaths(
    long CalcioUserPhotoId,
    string OriginalBlobName,
    string? SmallBlobName,
    string? MediumBlobName,
    string? LargeBlobName);

/// <summary>
/// Provides Calcio Users Service operations.
/// </summary>
/// <param name="readOnlyDbContextFactory">The read Only Db Context Factory.</param>
/// <param name="readWriteDbContextFactory">The read Write Db Context Factory.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="userClubsCacheService">The user Clubs Cache Service.</param>
/// <param name="blobStorageService">The blob Storage Service.</param>
/// <param name="cache">The cache.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
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
    /// <summary>
    /// Stores the Container Name.
    /// </summary>
    private const string ContainerName = "user-photos";
    /// <summary>
    /// Stores the Small Size.
    /// </summary>
    private const int SmallSize = 128;
    /// <summary>
    /// Stores the Medium Size.
    /// </summary>
    private const int MediumSize = 512;
    /// <summary>
    /// Stores the Large Size.
    /// </summary>
    private const int LargeSize = 1024;
    /// <summary>
    /// Stores the Sas Url Expiration.
    /// </summary>
    private static readonly TimeSpan SasUrlExpiration = TimeSpan.FromHours(1);
    /// <summary>
    /// Executes the Get Club Members Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Remove Club Member Async operation.
    /// </summary>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Log Member Removed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club Id.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="removingUserId">The removing User Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Member {UserId} removed from club {ClubId} by user {RemovingUserId}")]
    /// <summary>
    /// Executes the log member removed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clubId">The club id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="removingUserId">The removing user id.</param>
    private static partial void LogMemberRemoved(ILogger logger, long clubId, long userId, long removingUserId);

    /// <summary>
    /// Executes the Log Role Removal Failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="roleName">The role Name.</param>
    /// <param name="errors">The errors.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove {RoleName} role from user {UserId}: {Errors}")]
    /// <summary>
    /// Executes the log role removal failed operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="roleName">The role name.</param>
    /// <param name="errors">The errors.</param>
    private static partial void LogRoleRemovalFailed(ILogger logger, long userId, string roleName, string errors);

    /// <summary>
    /// Executes the Upload Account Photo Async operation.
    /// </summary>
    /// <param name="photoStream">The photo Stream.</param>
    /// <param name="contentType">The content Type.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Has Account Photo Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ServiceResult<bool>> HasAccountPhotoAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var hasPhoto = await dbContext.CalcioUserPhotos
            .AnyAsync(p => p.CalcioUserId == CurrentUserId, cancellationToken);

        return hasPhoto;
    }

    /// <summary>
    /// Executes the Generate Photo Dto operation.
    /// </summary>
    /// <param name="photo">The photo.</param>
    /// <returns>The operation result.</returns>
    private CalcioUserPhotoDto GeneratePhotoDto(CalcioUserPhotoEntity photo)
        => new(
            photo.CalcioUserPhotoId,
            blobStorageService.GetSasUrl(ContainerName, photo.OriginalBlobName, SasUrlExpiration).ToString(),
            photo.SmallBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.SmallBlobName, SasUrlExpiration).ToString() : null,
            photo.MediumBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.MediumBlobName, SasUrlExpiration).ToString() : null,
            photo.LargeBlobName is not null ? blobStorageService.GetSasUrl(ContainerName, photo.LargeBlobName, SasUrlExpiration).ToString() : null);

    /// <summary>
    /// Executes the Generate Photo Dto From Paths operation.
    /// </summary>
    /// <param name="paths">The paths.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Crop To Square operation.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Resize Bitmap operation.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="maxSize">The max Size.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Encode To Jpeg operation.
    /// </summary>
    /// <param name="bitmap">The bitmap.</param>
    /// <param name="quality">The quality.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Get Blob Guid From Path operation.
    /// </summary>
    /// <param name="blobPath">The blob Path.</param>
    /// <returns>The operation result.</returns>
    private static string GetBlobGuidFromPath(string blobPath)
    {
        // Path format: users/{userId}/{guid}/variant.jpg
        var parts = blobPath.Split('/');
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    /// <summary>
    /// Executes the Log Photo Uploaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="photoId">The photo Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded photo {PhotoId} for user {UserId}")]
    /// <summary>
    /// Executes the log photo uploaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="photoId">The photo id.</param>
    private static partial void LogPhotoUploaded(ILogger logger, long userId, long photoId);

    /// <summary>
    /// Executes the Log Existing Photo Deleted operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted existing photo for user {UserId}")]
    /// <summary>
    /// Executes the log existing photo deleted operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogExistingPhotoDeleted(ILogger logger, long userId);
}
