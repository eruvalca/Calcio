using System.Security.Claims;

using Azure.Storage.Blobs;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.Services.Players;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

using SkiaSharp;

namespace Calcio.IntegrationTests.Services.BlobStorage;

/// <summary>
/// Full integration tests for player photo upload with actual Azure Blob Storage emulator (Azurite).
/// These tests verify end-to-end blob storage operations including upload, download, and SAS URL generation.
/// </summary>
public class PlayerPhotoUploadFullIntegrationTests(BlobStorageApplicationFactory factory)
    : IClassFixture<BlobStorageApplicationFactory>, IAsyncLifetime
{
    private readonly BlobStorageApplicationFactory _factory = factory;
    private const long UserAId = 1;

    #region Test Lifecycle

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        if (!await dbContext.Users.AnyAsync())
        {
            await SeedDataAsync(dbContext);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #endregion

    #region Full Integration Tests

    [Fact]
    public async Task UploadPlayerPhoto_FullFlow_ShouldUploadToBlobStorage_AndGenerateSasUrls()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        using var photoStream = new MemoryStream(CreateTestJpegBytes(200, 200));

        // Act
        var result = await service.UploadPlayerPhotoAsync(
            club.ClubId,
            player.PlayerId,
            photoStream,
            "image/jpeg",
            cancellationToken);

        // Assert - Service result
        result.IsSuccess.ShouldBeTrue();
        var photoDto = result.Value;
        photoDto.PlayerPhotoId.ShouldBeGreaterThan(0);

        // Assert - SAS URLs are valid URIs
        new Uri(photoDto.OriginalUrl).ShouldNotBeNull();
        new Uri(photoDto.SmallUrl!).ShouldNotBeNull();
        new Uri(photoDto.MediumUrl!).ShouldNotBeNull();
        new Uri(photoDto.LargeUrl!).ShouldNotBeNull();

        // Assert - Blobs actually exist in Azurite
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("player-photos");

        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.PlayerPhotos
            .FirstAsync(p => p.PlayerPhotoId == photoDto.PlayerPhotoId, cancellationToken);

        var originalBlob = containerClient.GetBlobClient(persistedPhoto.OriginalBlobName);
        var smallBlob = containerClient.GetBlobClient(persistedPhoto.SmallBlobName!);
        var mediumBlob = containerClient.GetBlobClient(persistedPhoto.MediumBlobName!);
        var largeBlob = containerClient.GetBlobClient(persistedPhoto.LargeBlobName!);

        (await originalBlob.ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await smallBlob.ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await mediumBlob.ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
        (await largeBlob.ExistsAsync(cancellationToken)).Value.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadPlayerPhoto_FullFlow_ShouldGenerateCorrectImageSizes()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Create a large image to ensure resizing happens
        using var photoStream = new MemoryStream(CreateTestJpegBytes(2000, 2000));

        // Act
        var result = await service.UploadPlayerPhotoAsync(
            club.ClubId,
            player.PlayerId,
            photoStream,
            "image/jpeg",
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Download blobs and verify sizes
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("player-photos");

        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.PlayerPhotos
            .FirstAsync(p => p.PlayerPhotoId == result.Value.PlayerPhotoId, cancellationToken);

        // Download and verify small image (128x128)
        var smallBlob = containerClient.GetBlobClient(persistedPhoto.SmallBlobName!);
        using var smallStream = new MemoryStream();
        await smallBlob.DownloadToAsync(smallStream, cancellationToken);
        smallStream.Position = 0;
        using var smallBitmap = SKBitmap.Decode(smallStream);
        smallBitmap.Width.ShouldBeLessThanOrEqualTo(128);
        smallBitmap.Height.ShouldBeLessThanOrEqualTo(128);

        // Download and verify medium image (512x512)
        var mediumBlob = containerClient.GetBlobClient(persistedPhoto.MediumBlobName!);
        using var mediumStream = new MemoryStream();
        await mediumBlob.DownloadToAsync(mediumStream, cancellationToken);
        mediumStream.Position = 0;
        using var mediumBitmap = SKBitmap.Decode(mediumStream);
        mediumBitmap.Width.ShouldBeLessThanOrEqualTo(512);
        mediumBitmap.Height.ShouldBeLessThanOrEqualTo(512);

        // Download and verify large image (1024x1024)
        var largeBlob = containerClient.GetBlobClient(persistedPhoto.LargeBlobName!);
        using var largeStream = new MemoryStream();
        await largeBlob.DownloadToAsync(largeStream, cancellationToken);
        largeStream.Position = 0;
        using var largeBitmap = SKBitmap.Decode(largeStream);
        largeBitmap.Width.ShouldBeLessThanOrEqualTo(1024);
        largeBitmap.Height.ShouldBeLessThanOrEqualTo(1024);
    }

    [Fact]
    public async Task UploadPlayerPhoto_WithPng_ShouldConvertToJpeg()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Create PNG image
        using var photoStream = new MemoryStream(CreateTestPngBytes(200, 200));

        // Act
        var result = await service.UploadPlayerPhotoAsync(
            club.ClubId,
            player.PlayerId,
            photoStream,
            "image/png",
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify blob is stored as JPEG
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("player-photos");

        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.PlayerPhotos
            .FirstAsync(p => p.PlayerPhotoId == result.Value.PlayerPhotoId, cancellationToken);

        // Download and check JPEG magic bytes
        var originalBlob = containerClient.GetBlobClient(persistedPhoto.OriginalBlobName);
        using var downloadStream = new MemoryStream();
        await originalBlob.DownloadToAsync(downloadStream, cancellationToken);
        downloadStream.Position = 0;

        var header = new byte[3];
        await downloadStream.ReadAsync(header.AsMemory(0, 3), cancellationToken);

        // JPEG files start with 0xFF 0xD8 0xFF
        header[0].ShouldBe((byte)0xFF);
        header[1].ShouldBe((byte)0xD8);
        header[2].ShouldBe((byte)0xFF);
    }

    [Fact]
    public async Task UploadPlayerPhoto_ReplacingExisting_ShouldDeleteOldBlobs()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Upload first photo
        using var firstPhotoStream = new MemoryStream(CreateTestJpegBytes(100, 100));
        var firstResult = await service.UploadPlayerPhotoAsync(
            club.ClubId, player.PlayerId, firstPhotoStream, "image/jpeg", cancellationToken);
        firstResult.IsSuccess.ShouldBeTrue();

        // Get first photo blob paths
        await using var firstVerifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var firstPhoto = await firstVerifyContext.PlayerPhotos
            .FirstAsync(p => p.PlayerPhotoId == firstResult.Value.PlayerPhotoId, cancellationToken);

        var firstOriginalBlobName = firstPhoto.OriginalBlobName;

        // Upload second photo (should replace first)
        using var secondPhotoStream = new MemoryStream(CreateTestJpegBytes(100, 100));
        var secondResult = await service.UploadPlayerPhotoAsync(
            club.ClubId, player.PlayerId, secondPhotoStream, "image/jpeg", cancellationToken);
        secondResult.IsSuccess.ShouldBeTrue();

        // Assert - Old blobs should be deleted
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("player-photos");

        var oldBlob = containerClient.GetBlobClient(firstOriginalBlobName);
        (await oldBlob.ExistsAsync(cancellationToken)).Value.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPlayerPhoto_AfterUpload_ShouldReturnValidSasUrls()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Upload photo first
        using var photoStream = new MemoryStream(CreateTestJpegBytes(200, 200));
        var uploadResult = await service.UploadPlayerPhotoAsync(
            club.ClubId, player.PlayerId, photoStream, "image/jpeg", cancellationToken);
        uploadResult.IsSuccess.ShouldBeTrue();

        // Act - Get the photo (should return fresh SAS URLs)
        var getResult = await service.GetPlayerPhotoAsync(club.ClubId, player.PlayerId, cancellationToken);

        // Assert
        getResult.IsSuccess.ShouldBeTrue();
        getResult.Value.IsT0.ShouldBeTrue();

        var photoDto = getResult.Value.AsT0;
        // SAS URLs contain signature (sig=), version (sv=), expiry (se=), and permissions (sp=) parameters
        photoDto.OriginalUrl.ShouldContain("sig=");
        photoDto.OriginalUrl.ShouldContain("sv=");
        photoDto.OriginalUrl.ShouldContain("se=");
        photoDto.OriginalUrl.ShouldContain("sp=");

        // Verify the SAS URL can actually download the blob
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(photoDto.OriginalUrl, cancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
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

    private static PlayersService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var blobStorageService = services.GetRequiredService<IBlobStorageService>();
        var cache = services.GetRequiredService<HybridCache>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<PlayersService>>();

        return new PlayersService(readOnlyFactory, readWriteFactory, blobStorageService, cache, httpContextAccessor, logger);
    }

    private static byte[] CreateTestJpegBytes(int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Blue);

        // Draw something distinguishable
        using var paint = new SKPaint { Color = SKColors.White };
        canvas.DrawCircle(width / 2, height / 2, Math.Min(width, height) / 4, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);

        return data.ToArray();
    }

    private static byte[] CreateTestPngBytes(int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Red);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private static async Task SeedDataAsync(ReadWriteDbContext dbContext)
    {
        // Create Users
        var userFaker = new Faker<CalcioUserEntity>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.UserName, f => f.Internet.Email());

        var userA = userFaker.Generate();
        userA.Id = UserAId;

        // Create Club
        var clubFaker = new Faker<ClubEntity>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.State, f => f.Address.State());

        var club = clubFaker.Generate();

        userA.Club = club;
        club.CalcioUsers.Add(userA);

        // Create Players
        var playerFaker = new Faker<PlayerEntity>()
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Past(20, DateTime.Today.AddYears(-10))))
            .RuleFor(p => p.GraduationYear, f => f.Date.Future(10).Year)
            .RuleFor(p => p.Club, club);

        var players = playerFaker.Generate(3);
        foreach (var player in players)
        {
            club.Players.Add(player);
        }

        dbContext.Users.Add(userA);
        dbContext.Clubs.Add(club);

        await dbContext.SaveChangesAsync();
    }

    #endregion
}
