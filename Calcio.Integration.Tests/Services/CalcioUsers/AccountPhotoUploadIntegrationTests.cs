using System.Security.Claims;

using Azure.Storage.Blobs;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.Entities;
using Calcio.Services.CalcioUsers;
using Calcio.Shared.Caching;
using Calcio.Shared.Services.BlobStorage;
using Calcio.Shared.Services.UserClubsCache;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

using SkiaSharp;

namespace Calcio.Integration.Tests.Services.CalcioUsers;

/// <summary>
/// Full integration tests for account photo upload using the real Azurite blob storage emulator.
/// These tests verify end-to-end operations including upload, blob persistence, SAS URL generation,
/// and photo replacement for the <see cref="CalcioUsersService"/> implementation.
/// </summary>
/// <remarks>
/// The <see cref="BlobStorageApplicationFactory"/> provides a real <see cref="BlobServiceClient"/>
/// backed by Azurite, replacing the mock <see cref="IBlobStorageService"/> registered in the test host.
/// This covers upload scenarios that cannot be exercised from bUnit because <c>InputFile</c>
/// requires a real browser file picker.
/// </remarks>
public class AccountPhotoUploadIntegrationTests(BlobStorageApplicationFactory factory)
    : IClassFixture<BlobStorageApplicationFactory>, IAsyncLifetime
{
    private readonly BlobStorageApplicationFactory _factory = factory;
    private const long UserAId = 1;

    #region Test Lifecycle

    /// <summary>
    /// Resets the database and purges the user photo cache so each test begins with a clean slate
    /// regardless of execution order within the shared <see cref="BlobStorageApplicationFactory"/> fixture.
    /// </summary>
    /// <returns>A value task that completes when initialization has finished.</returns>
    public async ValueTask InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        // Purge any cached user photo paths left by a previous test in this run so that
        // GetAccountPhotoAsync always hits the DB after a reset rather than returning stale data.
        var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
        await cache.RemoveAsync(CacheDefaults.CalcioUsers.GetPhotoPathsKey(UserAId));

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        await SeedDataAsync(dbContext);
    }

    /// <summary>
    /// No explicit teardown required; the factory's <c>DisposeAsync</c> drops the database.
    /// </summary>
    /// <returns>A completed value task.</returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #endregion

    #region UploadAccountPhotoAsync Tests

    /// <summary>
    /// Verifies that uploading an account photo persists all image variants as blobs in Azurite
    /// and returns valid SAS URLs for each variant.
    /// </summary>
    [Fact]
    public async Task UploadAccountPhotoAsync_ValidImage_ShouldPersistBlobsInAzuriteAndReturnSasUrls()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        using var photoStream = new MemoryStream(CreateTestJpegBytes(400, 400));

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", cancellationToken);

        // Assert - service result
        result.IsSuccess.ShouldBeTrue();
        var photoDto = result.Value;
        photoDto.CalcioUserPhotoId.ShouldBeGreaterThan(0);

        // Assert - all returned URLs are well-formed
        new Uri(photoDto.OriginalUrl).ShouldNotBeNull();
        new Uri(photoDto.SmallUrl!).ShouldNotBeNull();
        new Uri(photoDto.MediumUrl!).ShouldNotBeNull();
        new Uri(photoDto.LargeUrl!).ShouldNotBeNull();

        // Assert - blobs actually exist in Azurite
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var container = blobServiceClient.GetBlobContainerClient("user-photos");

        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.CalcioUserPhotos
            .FirstAsync(p => p.CalcioUserPhotoId == photoDto.CalcioUserPhotoId, cancellationToken);

        (await container.GetBlobClient(persistedPhoto.OriginalBlobName).ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await container.GetBlobClient(persistedPhoto.SmallBlobName!).ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await container.GetBlobClient(persistedPhoto.MediumBlobName!).ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await container.GetBlobClient(persistedPhoto.LargeBlobName!).ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that the uploaded image variants conform to the expected maximum dimensions.
    /// </summary>
    [Fact]
    public async Task UploadAccountPhotoAsync_LargeImage_ShouldResizeVariantsToCorrectDimensions()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        using var photoStream = new MemoryStream(CreateTestJpegBytes(2000, 2000));

        // Act
        var result = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var container = blobServiceClient.GetBlobContainerClient("user-photos");

        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.CalcioUserPhotos
            .FirstAsync(p => p.CalcioUserPhotoId == result.Value.CalcioUserPhotoId, cancellationToken);

        // Download and verify small variant (128 px max)
        var smallStream = new MemoryStream();
        await container.GetBlobClient(persistedPhoto.SmallBlobName).DownloadToAsync(smallStream, cancellationToken);
        smallStream.Position = 0;
        using var smallBitmap = SKBitmap.Decode(smallStream);
        smallBitmap.Width.ShouldBeLessThanOrEqualTo(128);
        smallBitmap.Height.ShouldBeLessThanOrEqualTo(128);

        // Download and verify medium variant (512 px max)
        var mediumStream = new MemoryStream();
        await container.GetBlobClient(persistedPhoto.MediumBlobName).DownloadToAsync(mediumStream, cancellationToken);
        mediumStream.Position = 0;
        using var mediumBitmap = SKBitmap.Decode(mediumStream);
        mediumBitmap.Width.ShouldBeLessThanOrEqualTo(512);
        mediumBitmap.Height.ShouldBeLessThanOrEqualTo(512);

        // Download and verify large variant (1024 px max)
        var largeStream = new MemoryStream();
        await container.GetBlobClient(persistedPhoto.LargeBlobName).DownloadToAsync(largeStream, cancellationToken);
        largeStream.Position = 0;
        using var largeBitmap = SKBitmap.Decode(largeStream);
        largeBitmap.Width.ShouldBeLessThanOrEqualTo(1024);
        largeBitmap.Height.ShouldBeLessThanOrEqualTo(1024);
    }

    /// <summary>
    /// Verifies that uploading a second photo deletes the first photo's blobs from blob storage.
    /// </summary>
    [Fact]
    public async Task UploadAccountPhotoAsync_WhenExistingPhoto_ShouldReplaceOldBlobsWithNew()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Upload first photo
        using var firstStream = new MemoryStream(CreateTestJpegBytes(200, 200));
        var firstResult = await service.UploadAccountPhotoAsync(firstStream, "image/jpeg", cancellationToken);
        firstResult.IsSuccess.ShouldBeTrue();

        await using var firstVerifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var firstPhoto = await firstVerifyContext.CalcioUserPhotos
            .FirstAsync(p => p.CalcioUserPhotoId == firstResult.Value.CalcioUserPhotoId, cancellationToken);
        var firstOriginalBlobName = firstPhoto.OriginalBlobName;

        // Act - upload second photo
        using var secondStream = new MemoryStream(CreateTestJpegBytes(200, 200));
        var secondResult = await service.UploadAccountPhotoAsync(secondStream, "image/jpeg", cancellationToken);
        secondResult.IsSuccess.ShouldBeTrue();

        // Assert - old blob no longer exists in Azurite
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var container = blobServiceClient.GetBlobContainerClient("user-photos");

        (await container.GetBlobClient(firstOriginalBlobName).ExistsAsync(cancellationToken)).Value.ShouldBeFalse();
    }

    #endregion

    #region GetAccountPhotoAsync Tests

    /// <summary>
    /// Verifies that <see cref="CalcioUsersService.GetAccountPhotoAsync"/> returns a photo DTO with
    /// valid SAS URLs after a successful upload.
    /// </summary>
    [Fact]
    public async Task GetAccountPhotoAsync_AfterUpload_ShouldReturnPhotoDtoWithSasUrls()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        using var photoStream = new MemoryStream(CreateTestJpegBytes(300, 300));
        var uploadResult = await service.UploadAccountPhotoAsync(photoStream, "image/jpeg", cancellationToken);
        uploadResult.IsSuccess.ShouldBeTrue();

        // Act
        var getResult = await service.GetAccountPhotoAsync(cancellationToken);

        // Assert
        getResult.IsSuccess.ShouldBeTrue();
        getResult.Value.IsT0.ShouldBeTrue();

        var photoDto = getResult.Value.AsT0;
        photoDto.CalcioUserPhotoId.ShouldBe(uploadResult.Value.CalcioUserPhotoId);
        photoDto.OriginalUrl.ShouldContain("sig=");
        photoDto.OriginalUrl.ShouldContain("sv=");
        photoDto.OriginalUrl.ShouldContain("se=");
        photoDto.OriginalUrl.ShouldContain("sp=");
    }

    /// <summary>
    /// Verifies that <see cref="CalcioUsersService.GetAccountPhotoAsync"/> returns <c>None</c>
    /// when the user has not uploaded a photo.
    /// </summary>
    [Fact]
    public async Task GetAccountPhotoAsync_WhenNoPhotoUploaded_ShouldReturnNone()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetAccountPhotoAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT1.ShouldBeTrue(); // T1 = None
    }

    #endregion

    #region Helpers

    private static void SetCurrentUser(IServiceProvider services, long userId)
    {
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext = new DefaultHttpContext { User = principal };
    }

    private static CalcioUsersService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var userManager = services.GetRequiredService<UserManager<CalcioUserEntity>>();
        var userClubsCacheService = services.GetRequiredService<IUserClubsCacheService>();
        var blobStorageService = services.GetRequiredService<IBlobStorageService>();
        var cache = services.GetRequiredService<HybridCache>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<CalcioUsersService>>();

        return new CalcioUsersService(readOnlyFactory, readWriteFactory, userManager, userClubsCacheService, blobStorageService, cache, httpContextAccessor, logger);
    }

    private static byte[] CreateTestJpegBytes(int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Blue);

        using var paint = new SKPaint { Color = SKColors.White };
        canvas.DrawCircle(width / 2, height / 2, Math.Min(width, height) / 4, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);

        return data.ToArray();
    }

    private static async Task SeedDataAsync(ReadWriteDbContext dbContext)
    {
        var userFaker = new Faker<CalcioUserEntity>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.UserName, f => f.Internet.Email());

        var userA = userFaker.Generate();
        userA.Id = UserAId;

        var clubFaker = new Faker<ClubEntity>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.State, f => f.Address.State());

        var club = clubFaker.Generate();
        userA.Club = club;
        club.CalcioUsers.Add(userA);

        await dbContext.Users.AddAsync(userA);
        await dbContext.Clubs.AddAsync(club);
        await dbContext.SaveChangesAsync();
    }

    #endregion
}
