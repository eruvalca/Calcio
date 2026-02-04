using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.IntegrationTests.Data.Interceptors;

public class AuditSaveChangesInterceptorTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long OriginalCreatorId = 999;

    [Fact]
    public async Task AddedEntity_PreservesCreatedById_SetByServiceLayer()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - CreatedById Preserved",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = OriginalCreatorId // Service layer sets this explicitly
        };

        // Act
        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - CreatedById should be preserved from what was set, not overwritten
        team.CreatedById.ShouldBe(OriginalCreatorId);
    }

    [Fact]
    public async Task AddedEntity_SetsCreatedAtTimestamp()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var beforeSave = DateTimeOffset.UtcNow;

        var team = new TeamEntity
        {
            Name = "Test Team - CreatedAt",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        // Act
        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var afterSave = DateTimeOffset.UtcNow;

        // Assert
        team.CreatedAt.ShouldBeInRange(beforeSave, afterSave);
    }

    [Fact]
    public async Task AddedEntity_SetsModifiedAtTimestamp()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var beforeSave = DateTimeOffset.UtcNow;

        var team = new TeamEntity
        {
            Name = "Test Team - ModifiedAt",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        // Act
        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var afterSave = DateTimeOffset.UtcNow;

        // Assert
        team.ModifiedAt.ShouldNotBeNull();
        team.ModifiedAt.Value.ShouldBeInRange(beforeSave, afterSave);
    }

    [Fact]
    public async Task AddedEntity_SetsModifiedByIdFromHttpContext()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - ModifiedById",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = OriginalCreatorId // Different from current user
        };

        // Act
        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - ModifiedById should come from HTTP context, not CreatedById
        team.ModifiedById.ShouldBe(UserAId);
    }

    [Fact]
    public async Task ModifiedEntity_UpdatesModifiedAtTimestamp()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - ModifiedAt Update",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalModifiedAt = team.ModifiedAt;

        // Small delay to ensure timestamp difference
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // Act
        team.Name = "Updated Team Name";
        var beforeUpdate = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        team.ModifiedAt.ShouldNotBeNull();
        team.ModifiedAt.Value.ShouldBeInRange(beforeUpdate, afterUpdate);
        team.ModifiedAt.ShouldNotBe(originalModifiedAt);
    }

    [Fact]
    public async Task ModifiedEntity_UpdatesModifiedById()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - ModifiedById Update",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        // Re-fetch and modify as a different user
        using var scope2 = Factory.Services.CreateScope();
        SetCurrentUser(scope2.ServiceProvider, UserBId);

        var dbContext2 = scope2.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var teamToUpdate = await dbContext2.Teams
            .IgnoreQueryFilters()
            .FirstAsync(t => t.TeamId == team.TeamId, TestContext.Current.CancellationToken);

        // Act
        teamToUpdate.Name = "Updated By User B";
        await dbContext2.SaveChangesAsync(TestContext.Current.CancellationToken);
        // Assert
        teamToUpdate.ModifiedById.ShouldBe(UserBId);
    }

    [Fact]
    public async Task ModifiedEntity_ProtectsCreatedAtFromChanges()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - CreatedAt Protection",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalCreatedAt = team.CreatedAt;
        var attemptedCreatedAt = DateTimeOffset.UtcNow.AddYears(-10);

        // Act - Try to change CreatedAt
        team.CreatedAt = attemptedCreatedAt;
        team.Name = "Name changed to trigger modified";
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Re-fetch to verify database value
        dbContext.ChangeTracker.Clear();
        var refetched = await dbContext.Teams
            .IgnoreQueryFilters()
            .FirstAsync(t => t.TeamId == team.TeamId, TestContext.Current.CancellationToken);

        // Assert - CreatedAt should not have changed to the attempted value
        refetched.CreatedAt.ShouldNotBe(attemptedCreatedAt);
        // CreatedAt should be close to the original (within a second, accounting for DB precision)
        Math.Abs((refetched.CreatedAt - originalCreatedAt).TotalSeconds).ShouldBeLessThan(1);
    }

    [Fact]
    public async Task ModifiedEntity_ProtectsCreatedByIdFromChanges()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - CreatedById Protection",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalCreatedById = team.CreatedById;

        // Act - Try to change CreatedById
        team.CreatedById = 9999;
        team.Name = "Name changed to trigger modified";
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Re-fetch to verify database value
        dbContext.ChangeTracker.Clear();
        var refetched = await dbContext.Teams
            .IgnoreQueryFilters()
            .FirstAsync(t => t.TeamId == team.TeamId, TestContext.Current.CancellationToken);

        // Assert - CreatedById should not have changed in the database
        refetched.CreatedById.ShouldBe(originalCreatedById);
    }

    [Fact]
    public async Task AddedEntity_CreatedAtAndModifiedAtAreEqual()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var club = await dbContext.Clubs.FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - Timestamps Equal",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        // Act
        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - On initial creation, CreatedAt and ModifiedAt should be equal
        team.CreatedAt.ShouldBe(team.ModifiedAt!.Value);
    }

    [Fact]
    public async Task SaveChanges_WithNoUser_ThrowsInvalidOperationException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        // Deliberately NOT setting a user

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // We need to get a club without query filters since no user is set
        var club = await dbContext.Clubs.IgnoreQueryFilters().FirstAsync(TestContext.Current.CancellationToken);

        var team = new TeamEntity
        {
            Name = "Test Team - No User",
            GraduationYear = 2030,
            ClubId = club.ClubId,
            CreatedById = 1 // Even though we set this, interceptor will fail on no HTTP context user
        };

        await dbContext.Teams.AddAsync(team, TestContext.Current.CancellationToken);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await dbContext.SaveChangesAsync());
    }
}
