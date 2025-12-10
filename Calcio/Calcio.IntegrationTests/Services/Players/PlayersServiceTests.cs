using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Players;
using Calcio.Shared.Services.BlobStorage;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

using SkiaSharp;

namespace Calcio.IntegrationTests.Services.Players;

public class PlayersServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    #region GetClubPlayersAsync Tests

    [Fact]
    public async Task GetClubPlayersAsync_WhenUserIsMember_ReturnsPlayers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;
        players.ShouldNotBeEmpty();
        players.All(p => p.PlayerId > 0).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.FirstName)).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.LastName)).ShouldBeTrue();
        players.All(p => !string.IsNullOrEmpty(p.FullName)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenUserIsNotMember_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club (which UserA is not a member of)
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(otherClub.ClubId, cancellationToken);

        // Assert - Global query filters return empty result for clubs user doesn't belong to
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldReturnPlayersOrderedByLastNameThenFirstName()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        if (players.Count > 1)
        {
            // Verify ordering: by last name, then by first name
            var sortedPlayers = players
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToList();

            players.ShouldBe(sortedPlayers);
        }
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldReturnCorrectPlayerData()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var expectedPlayer = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        var player = players.FirstOrDefault(p => p.PlayerId == expectedPlayer.PlayerId);
        player.ShouldNotBeNull();
        player.FirstName.ShouldBe(expectedPlayer.FirstName);
        player.LastName.ShouldBe(expectedPlayer.LastName);
        player.FullName.ShouldBe(expectedPlayer.FullName);
        player.DateOfBirth.ShouldBe(expectedPlayer.DateOfBirth);
        player.Gender.ShouldBe(expectedPlayer.Gender);
        player.JerseyNumber.ShouldBe(expectedPlayer.JerseyNumber);
        player.TryoutNumber.ShouldBe(expectedPlayer.TryoutNumber);
    }

    [Fact]
    public async Task GetClubPlayersAsync_WhenClubDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var service = CreateService(scope.ServiceProvider);

        // Act
        var result = await service.GetClubPlayersAsync(999999, cancellationToken);

        // Assert - Global query filters return empty result for non-existent clubs
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetClubPlayersAsync_ShouldOnlyReturnPlayersForSpecifiedClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubPlayersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var players = result.Value;

        // Verify all returned players belong to the specified club
        // The query filter ensures this, but we can verify via the database
        var clubPlayerIds = await dbContext.Players
            .Where(p => p.ClubId == club.ClubId)
            .Select(p => p.PlayerId)
            .ToListAsync(cancellationToken);

        players.All(p => clubPlayerIds.Contains(p.PlayerId)).ShouldBeTrue();
    }

    #endregion

    #region CreatePlayerAsync Tests

    [Fact]
    public async Task CreatePlayerAsync_WhenValidData_ReturnsCreatedPlayer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var dto = new Shared.DTOs.Players.CreatePlayerDto(
            FirstName: "Test",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-15)),
            GraduationYear: DateTime.Today.Year + 3,
            Gender: Shared.Enums.Gender.Male,
            JerseyNumber: 10,
            TryoutNumber: 100);

        // Act
        var result = await service.CreatePlayerAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var created = result.Value;
        created.PlayerId.ShouldBeGreaterThan(0);
        created.FirstName.ShouldBe("Test");
        created.LastName.ShouldBe("Player");
        created.FullName.ShouldBe("Test Player");
    }

    [Fact]
    public async Task CreatePlayerAsync_WhenMinimalData_ReturnsCreatedPlayer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var dto = new Shared.DTOs.Players.CreatePlayerDto(
            FirstName: "Minimal",
            LastName: "Data",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-12)),
            GraduationYear: DateTime.Today.Year + 6);

        // Act
        var result = await service.CreatePlayerAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var created = result.Value;
        created.PlayerId.ShouldBeGreaterThan(0);
        created.FirstName.ShouldBe("Minimal");
        created.LastName.ShouldBe("Data");
    }

    [Fact]
    public async Task CreatePlayerAsync_ShouldPersistPlayerToDatabase()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readOnlyContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await readOnlyContext.Clubs.FirstAsync(cancellationToken);

        var dto = new Shared.DTOs.Players.CreatePlayerDto(
            FirstName: "Persisted",
            LastName: "Player",
            DateOfBirth: DateOnly.FromDateTime(DateTime.Today.AddYears(-14)),
            GraduationYear: DateTime.Today.Year + 4,
            Gender: Shared.Enums.Gender.Female,
            JerseyNumber: 7,
            TryoutNumber: 42);

        // Act
        var result = await service.CreatePlayerAsync(club.ClubId, dto, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify persistence using a fresh context
        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPlayer = await verifyContext.Players
            .FirstOrDefaultAsync(p => p.PlayerId == result.Value.PlayerId, cancellationToken);

        persistedPlayer.ShouldNotBeNull();
        persistedPlayer.FirstName.ShouldBe("Persisted");
        persistedPlayer.LastName.ShouldBe("Player");
        persistedPlayer.DateOfBirth.ShouldBe(dto.DateOfBirth);
        persistedPlayer.GraduationYear.ShouldBe(dto.GraduationYear);
        persistedPlayer.Gender.ShouldBe(Shared.Enums.Gender.Female);
        persistedPlayer.JerseyNumber.ShouldBe(7);
        persistedPlayer.TryoutNumber.ShouldBe(42);
        persistedPlayer.ClubId.ShouldBe(club.ClubId);
        persistedPlayer.CreatedById.ShouldBe(UserAId);
    }

    #endregion

    #region GetPlayerPhotoAsync Tests

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenNoPhoto_ReturnsNone()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Act
        var result = await service.GetPlayerPhotoAsync(club.ClubId, player.PlayerId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT1.ShouldBeTrue(); // None
    }

    [Fact]
    public async Task GetPlayerPhotoAsync_WhenPlayerNotFound_ReturnsNone()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetPlayerPhotoAsync(club.ClubId, 999999, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsT1.ShouldBeTrue(); // None
    }

    #endregion

    #region UploadPlayerPhotoAsync Tests

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenPlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        using var photoStream = new MemoryStream(CreateMinimalJpegBytes());

        // Act
        var result = await service.UploadPlayerPhotoAsync(club.ClubId, 999999, photoStream, "image/jpeg", cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(Shared.Results.ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WhenPlayerInDifferentClub_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Get a player from a different club
        var otherClubPlayer = await dbContext.Players
            .IgnoreQueryFilters()
            .FirstAsync(p => p.ClubId != club.ClubId, cancellationToken);

        using var photoStream = new MemoryStream(CreateMinimalJpegBytes());

        // Act
        var result = await service.UploadPlayerPhotoAsync(club.ClubId, otherClubPlayer.PlayerId, photoStream, "image/jpeg", cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(Shared.Results.ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_ShouldPersistPhotoToDatabase_WithCorrectBlobPaths()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        using var photoStream = new MemoryStream(CreateMinimalJpegBytes());

        // Act
        var result = await service.UploadPlayerPhotoAsync(club.ClubId, player.PlayerId, photoStream, "image/jpeg", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var photoDto = result.Value;
        photoDto.PlayerPhotoId.ShouldBeGreaterThan(0);
        photoDto.OriginalUrl.ShouldNotBeNullOrEmpty();
        photoDto.SmallUrl.ShouldNotBeNullOrEmpty();
        photoDto.MediumUrl.ShouldNotBeNullOrEmpty();
        photoDto.LargeUrl.ShouldNotBeNullOrEmpty();

        // Verify persistence using a fresh context
        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.PlayerPhotos
            .FirstOrDefaultAsync(p => p.PlayerPhotoId == photoDto.PlayerPhotoId, cancellationToken);

        persistedPhoto.ShouldNotBeNull();
        persistedPhoto.PlayerId.ShouldBe(player.PlayerId);
        persistedPhoto.ClubId.ShouldBe(club.ClubId);
        persistedPhoto.ContentType.ShouldBe("image/jpeg");
        persistedPhoto.CreatedById.ShouldBe(UserAId);

        // Verify blob paths follow expected format: players/{playerId}/{guid}/{variant}.jpg
        persistedPhoto.OriginalBlobName.ShouldStartWith($"players/{player.PlayerId}/");
        persistedPhoto.OriginalBlobName.ShouldEndWith("/original.jpg");
        persistedPhoto.SmallBlobName.ShouldNotBeNull();
        persistedPhoto.SmallBlobName.ShouldEndWith("/small.jpg");
        persistedPhoto.MediumBlobName.ShouldNotBeNull();
        persistedPhoto.MediumBlobName.ShouldEndWith("/medium.jpg");
        persistedPhoto.LargeBlobName.ShouldNotBeNull();
        persistedPhoto.LargeBlobName.ShouldEndWith("/large.jpg");

        // Verify all variants share the same GUID path
        var originalGuid = GetBlobGuidFromPath(persistedPhoto.OriginalBlobName);
        GetBlobGuidFromPath(persistedPhoto.SmallBlobName).ShouldBe(originalGuid);
        GetBlobGuidFromPath(persistedPhoto.MediumBlobName).ShouldBe(originalGuid);
        GetBlobGuidFromPath(persistedPhoto.LargeBlobName).ShouldBe(originalGuid);

        // Verify player's primary photo reference was updated
        var updatedPlayer = await verifyContext.Players
            .FirstAsync(p => p.PlayerId == player.PlayerId, cancellationToken);
        updatedPlayer.PrimaryPhotoBlobName.ShouldBe(persistedPhoto.OriginalBlobName);
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_WithPngInput_ShouldNormalizeToJpeg()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Create PNG instead of JPEG
        using var photoStream = new MemoryStream(CreateMinimalPngBytes());

        // Act - pass PNG content type
        var result = await service.UploadPlayerPhotoAsync(club.ClubId, player.PlayerId, photoStream, "image/png", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify persistence - should be stored as JPEG regardless of input
        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var persistedPhoto = await verifyContext.PlayerPhotos
            .FirstOrDefaultAsync(p => p.PlayerPhotoId == result.Value.PlayerPhotoId, cancellationToken);

        persistedPhoto.ShouldNotBeNull();
        persistedPhoto.ContentType.ShouldBe("image/jpeg"); // Normalized to JPEG
        persistedPhoto.OriginalBlobName.ShouldEndWith(".jpg");
    }

    [Fact]
    public async Task UploadPlayerPhotoAsync_ShouldReplaceExistingPhoto()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);
        var player = await dbContext.Players.FirstAsync(p => p.ClubId == club.ClubId, cancellationToken);

        // Upload first photo
        using var firstPhotoStream = new MemoryStream(CreateMinimalJpegBytes());
        var firstResult = await service.UploadPlayerPhotoAsync(club.ClubId, player.PlayerId, firstPhotoStream, "image/jpeg", cancellationToken);
        firstResult.IsSuccess.ShouldBeTrue();
        var firstPhotoId = firstResult.Value.PlayerPhotoId;

        // Upload second photo (should replace the first)
        using var secondPhotoStream = new MemoryStream(CreateMinimalJpegBytes());
        var secondResult = await service.UploadPlayerPhotoAsync(club.ClubId, player.PlayerId, secondPhotoStream, "image/jpeg", cancellationToken);

        // Assert
        secondResult.IsSuccess.ShouldBeTrue();
        var secondPhotoId = secondResult.Value.PlayerPhotoId;
        secondPhotoId.ShouldNotBe(firstPhotoId); // New photo entity created

        // Verify only the new photo exists in database
        await using var verifyContext = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>()
            .CreateDbContextAsync(cancellationToken);

        var playerPhotos = await verifyContext.PlayerPhotos
            .Where(p => p.PlayerId == player.PlayerId)
            .ToListAsync(cancellationToken);

        playerPhotos.Count.ShouldBe(1);
        playerPhotos[0].PlayerPhotoId.ShouldBe(secondPhotoId);
    }

    #endregion

    #region Helpers

    private static string GetBlobGuidFromPath(string? blobPath)
    {
        if (string.IsNullOrEmpty(blobPath))
        {
            return string.Empty;
        }

        // Path format: players/{playerId}/{guid}/variant.jpg
        var parts = blobPath.Split('/');
        return parts.Length >= 3 ? parts[2] : string.Empty;
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

    /// <summary>
    /// Creates a minimal valid JPEG byte array for testing image processing.
    /// This creates a tiny valid JPEG that SkiaSharp can decode.
    /// </summary>
    private static byte[] CreateMinimalJpegBytes()
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(100, 100));
        using var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Blue);

        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 75);

        return data.ToArray();
    }

    /// <summary>
    /// Creates a minimal valid PNG byte array for testing image format normalization.
    /// </summary>
    private static byte[] CreateMinimalPngBytes()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Red);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    #endregion
}
