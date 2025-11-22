using Calcio.Data.Contexts;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.IntegrationTests.Data.Contexts;

public class ReadWriteDbContextTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    [Fact]
    public async Task GetClubs_ShouldReturnOnlyUserClubs()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var clubs = await context.Clubs
            .Include(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        clubs.ShouldNotBeEmpty();
        clubs.ShouldAllBe(c => c.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetSeasons_ShouldReturnOnlyUserSeasons()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var seasons = await context.Seasons
            .Include(s => s.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        seasons.ShouldNotBeEmpty();
        seasons.ShouldAllBe(s => s.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetCampaigns_ShouldReturnOnlyUserCampaigns()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var campaigns = await context.Campaigns
            .Include(c => c.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        campaigns.ShouldNotBeEmpty();
        campaigns.ShouldAllBe(c => c.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetTeams_ShouldReturnOnlyUserTeams()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var teams = await context.Teams
            .Include(t => t.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        teams.ShouldNotBeEmpty();
        teams.ShouldAllBe(t => t.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetPlayers_ShouldReturnOnlyUserPlayers()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var players = await context.Players
            .Include(p => p.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        players.ShouldNotBeEmpty();
        players.ShouldAllBe(p => p.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetNotes_ShouldReturnOnlyUserNotes()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        await SeedNotesForBothClubsAsync(context, TestContext.Current.CancellationToken);

        // Act
        var notes = await context.Notes
            .Include(n => n.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        notes.ShouldNotBeEmpty();
        notes.ShouldAllBe(n => n.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetPlayerTags_ShouldReturnOnlyUserTags()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        await SeedPlayerTagsForBothClubsAsync(context, TestContext.Current.CancellationToken);

        // Act
        var tags = await context.PlayerTags
            .Include(t => t.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        tags.ShouldNotBeEmpty();
        tags.ShouldAllBe(t => t.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetPlayerCampaignAssignments_ShouldReturnOnlyUserAssignments()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        await SeedCampaignAssignmentsForBothClubsAsync(context, TestContext.Current.CancellationToken);

        // Act
        var assignments = await context.PlayerCampaignAssignments
            .Include(a => a.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        assignments.ShouldNotBeEmpty();
        assignments.ShouldAllBe(a => a.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task GetPlayerPhotos_ShouldReturnOnlyUserPhotos()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        await SeedPlayerPhotosForBothClubsAsync(context, TestContext.Current.CancellationToken);

        // Act
        var photos = await context.PlayerPhotos
            .Include(p => p.Club)
            .ThenInclude(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        photos.ShouldNotBeEmpty();
        photos.ShouldAllBe(p => p.Club.CalcioUsers.Any(u => u.Id == UserAId));
    }

    [Fact]
    public async Task QueryWithoutAuthenticatedUser_ShouldReturnEmptyResults()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        // Act
        var clubs = await context.Clubs
            .Include(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        clubs.ShouldBeEmpty();
    }
}
