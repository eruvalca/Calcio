using System.Text;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.Players;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Enums;
using Calcio.Shared.Results;
using Calcio.Shared.Services.BlobStorage;
using Calcio.Shared.Services.Players;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.Players;

public class BulkImportPlayersTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    #region ValidateBulkImportAsync Tests

    [Fact]
    public async Task ValidateBulkImportAsync_WithValidCsv_ReturnsValidRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var csvContent = """
            first_name,last_name,date_of_birth,gender,graduation_year
            John,Doe,2010-05-15,Male,2028
            Jane,Smith,2011-03-22,Female,2029
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.Rows.Count.ShouldBe(2);
        validation.ValidCount.ShouldBe(2);
        validation.ErrorCount.ShouldBe(0);
        validation.MissingRequiredColumns.ShouldBeEmpty();

        validation.Rows[0].FirstName.ShouldBe("John");
        validation.Rows[0].LastName.ShouldBe("Doe");
        validation.Rows[0].Gender.ShouldBe(Gender.Male);
        validation.Rows[0].IsValid.ShouldBeTrue();

        validation.Rows[1].FirstName.ShouldBe("Jane");
        validation.Rows[1].LastName.ShouldBe("Smith");
        validation.Rows[1].Gender.ShouldBe(Gender.Female);
        validation.Rows[1].IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBulkImportAsync_WithMissingRequiredColumns_ReturnsError()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Missing gender column
        var csvContent = """
            first_name,last_name,date_of_birth
            John,Doe,2010-05-15
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.MissingRequiredColumns.ShouldContain("Gender");
    }

    [Fact]
    public async Task ValidateBulkImportAsync_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var csvContent = """
            first_name,last_name,date_of_birth,gender,graduation_year
            John,Doe,2010-05-15,Male,2028
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.xlsx", cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.BadRequest);
    }

    [Fact]
    public async Task ValidateBulkImportAsync_WithInvalidData_ReturnsRowErrors()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var csvContent = """
            first_name,last_name,date_of_birth,gender
            ,Doe,2010-05-15,Male
            John,,invalid-date,InvalidGender
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.Rows.Count.ShouldBe(2);
        validation.ErrorCount.ShouldBe(2);

        // First row missing first name
        validation.Rows[0].IsValid.ShouldBeFalse();
        validation.Rows[0].Errors.ShouldContain(e => e.Contains("First name"));

        // Second row has multiple errors
        validation.Rows[1].IsValid.ShouldBeFalse();
        validation.Rows[1].Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ValidateBulkImportAsync_WithMissingGraduationYear_ComputesFromDob()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // No graduation year column
        var csvContent = """
            first_name,last_name,date_of_birth,gender
            John,Doe,2010-05-15,Male
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.Rows[0].GraduationYear.ShouldBe(2028); // Born May 2010 -> 2028
        validation.Rows[0].IsGraduationYearComputed.ShouldBeTrue();
        validation.Rows[0].Warnings.ShouldContain(w => w.Contains("Graduation year computed"));
    }

    [Fact]
    public async Task ValidateBulkImportAsync_WithDuplicatesInFile_AddsWarning()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Same player twice
        var csvContent = """
            first_name,last_name,date_of_birth,gender
            John,Doe,2010-05-15,Male
            John,Doe,2010-05-15,Male
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.DuplicateInFileCount.ShouldBe(1);
        validation.Rows[1].IsDuplicateInFile.ShouldBeTrue();
        validation.Rows[1].Warnings.ShouldContain(w => w.Contains("Duplicate"));
    }

    #endregion

    #region BulkCreatePlayersAsync Tests

    [Fact]
    public async Task BulkCreatePlayersAsync_WithValidRows_CreatesPlayers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var writeDbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await readDbContext.Clubs.FirstAsync(cancellationToken);

        var initialPlayerCount = await readDbContext.Players.CountAsync(cancellationToken);

        var rows = new List<PlayerImportRowDto>
        {
            new()
            {
                RowNumber = 1,
                FirstName = "BulkTest",
                LastName = "PlayerOne",
                DateOfBirth = new DateOnly(2012, 6, 15),
                Gender = Gender.Male,
                GraduationYear = 2030,
                IsMarkedForImport = true
            },
            new()
            {
                RowNumber = 2,
                FirstName = "BulkTest",
                LastName = "PlayerTwo",
                DateOfBirth = new DateOnly(2011, 9, 20),
                Gender = Gender.Female,
                GraduationYear = 2030,
                IsMarkedForImport = true
            },
            new()
            {
                RowNumber = 3,
                FirstName = "Skipped",
                LastName = "Player",
                DateOfBirth = new DateOnly(2010, 1, 1),
                Gender = Gender.Male,
                GraduationYear = 2028,
                IsMarkedForImport = false // Not marked for import
            }
        };

        // Act
        var result = await service.BulkCreatePlayersAsync(club.ClubId, rows, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var importResult = result.Value;
        importResult.CreatedCount.ShouldBe(2);
        importResult.SkippedCount.ShouldBe(1);

        // Verify players were created in database
        var newPlayerCount = await readDbContext.Players.CountAsync(cancellationToken);
        newPlayerCount.ShouldBe(initialPlayerCount + 2);

        var createdPlayer = await readDbContext.Players
            .FirstOrDefaultAsync(p => p.FirstName == "BulkTest" && p.LastName == "PlayerOne", cancellationToken);
        createdPlayer.ShouldNotBeNull();
        createdPlayer.DateOfBirth.ShouldBe(new DateOnly(2012, 6, 15));
        createdPlayer.Gender.ShouldBe(Gender.Male);
        createdPlayer.GraduationYear.ShouldBe(2030);
    }

    [Fact]
    public async Task BulkCreatePlayersAsync_WithInvalidRows_SkipsInvalidRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await readDbContext.Clubs.FirstAsync(cancellationToken);

        var rows = new List<PlayerImportRowDto>
        {
            new()
            {
                RowNumber = 1,
                FirstName = "Valid",
                LastName = "Player",
                DateOfBirth = new DateOnly(2012, 6, 15),
                Gender = Gender.Male,
                GraduationYear = 2030,
                IsMarkedForImport = true
                // No errors = valid
            },
            new()
            {
                RowNumber = 2,
                FirstName = "Invalid",
                LastName = "Player",
                DateOfBirth = new DateOnly(2011, 9, 20),
                Gender = Gender.Female,
                GraduationYear = 2030,
                IsMarkedForImport = true,
                Errors = ["Some validation error"] // Has errors = invalid
            }
        };

        // Act
        var result = await service.BulkCreatePlayersAsync(club.ClubId, rows, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var importResult = result.Value;
        importResult.CreatedCount.ShouldBe(1);
        importResult.SkippedCount.ShouldBe(1);
    }

    #endregion

    #region RevalidateBulkImportAsync Tests

    [Fact]
    public async Task RevalidateBulkImportAsync_WithCorrectedData_UpdatesValidation()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var readDbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await readDbContext.Clubs.FirstAsync(cancellationToken);

        // Start with an invalid row that was corrected
        var rows = new List<PlayerImportRowDto>
        {
            new()
            {
                RowNumber = 1,
                FirstName = "John", // Was originally empty, now corrected
                LastName = "Doe",
                DateOfBirth = new DateOnly(2010, 5, 15),
                Gender = Gender.Male,
                GraduationYear = 2028,
                Errors = ["First name is required"] // Previous errors, will be cleared on revalidation
            }
        };

        // Act
        var result = await service.RevalidateBulkImportAsync(club.ClubId, rows, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.Rows[0].IsValid.ShouldBeTrue();
        validation.Rows[0].Errors.ShouldBeEmpty();
        validation.ValidCount.ShouldBe(1);
        validation.ErrorCount.ShouldBe(0);
    }

    #endregion

    #region Column Alias Detection Tests

    [Theory]
    [InlineData("FirstName,LastName,DOB,Sex", true)]
    [InlineData("first_name,last_name,date_of_birth,gender", true)]
    [InlineData("First,Last,Birthday,Gender", true)]
    [InlineData("given_name,surname,birth_date,sex", true)]
    public async Task ValidateBulkImportAsync_WithVariousColumnFormats_DetectsColumns(string headerLine, bool shouldSucceed)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        var csvContent = $"{headerLine}\nJohn,Doe,2010-05-15,Male";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateBulkImportAsync(club.ClubId, stream, "players.csv", cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var validation = result.Value;
        validation.MissingRequiredColumns.ShouldBeEmpty("All required columns should be detected with aliases");
        if (shouldSucceed)
        {
            validation.Rows[0].IsValid.ShouldBeTrue();
        }
    }

    #endregion

    private static PlayersService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var blobStorageService = services.GetRequiredService<IBlobStorageService>();
        var importParserService = services.GetRequiredService<IPlayerImportParserService>();
        var cache = services.GetRequiredService<HybridCache>();
        var httpContextAccessor = services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<PlayersService>>();

        return new PlayersService(readOnlyFactory, readWriteFactory, blobStorageService, importParserService, cache, httpContextAccessor, logger);
    }
}
