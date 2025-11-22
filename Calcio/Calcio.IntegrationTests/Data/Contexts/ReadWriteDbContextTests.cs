using Calcio.Data.Contexts;

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
}
