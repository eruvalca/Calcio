using System.Security.Claims;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.IntegrationTests.Data.Contexts;

public class ReadWriteDbContextTests(CustomApplicationFactory factory) : IClassFixture<CustomApplicationFactory>, IAsyncLifetime
{
    private readonly CustomApplicationFactory _factory = factory;
    private const long UserAId = 1;
    private const long UserBId = 2;

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        // Set a user for seeding (auditing)
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        if (!await dbContext.Users.AnyAsync())
        {
            await SeedDataAsync(dbContext);
        }
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    private static async Task SeedDataAsync(ReadWriteDbContext dbContext)
    {
        // Create Users
        var userFaker = new Faker<CalcioUserEntity>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.EmailAddress, f => f.Internet.Email())
            .RuleFor(u => u.UserName, (f, u) => u.EmailAddress);

        var userA = userFaker.Generate();
        userA.Id = UserAId;

        var userB = userFaker.Generate();
        userB.Id = UserBId;

        // Create Clubs
        var clubFaker = new Faker<ClubEntity>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.State, f => f.Address.State());

        var clubA = clubFaker.Generate();
        var clubB = clubFaker.Generate();

        // Link Users to Clubs
        userA.Club = clubA;
        clubA.CalcioUsers.Add(userA);

        userB.Club = clubB;
        clubB.CalcioUsers.Add(userB);

        // Generate entities for Club A
        GenerateClubEntities(clubA);

        // Generate entities for Club B
        GenerateClubEntities(clubB);

        dbContext.Users.AddRange(userA, userB);
        dbContext.Clubs.AddRange(clubA, clubB);

        await dbContext.SaveChangesAsync();
    }

    private static void GenerateClubEntities(ClubEntity club)
    {
        var seasonFaker = new Faker<SeasonEntity>()
            .RuleFor(s => s.Name, f => $"{f.Date.Past().Year}-{f.UniqueIndex}")
            .RuleFor(s => s.StartDate, f => DateOnly.FromDateTime(f.Date.Past()))
            .RuleFor(s => s.Club, club);

        var seasons = seasonFaker.Generate(2);
        foreach (var s in seasons)
        {
            club.Seasons.Add(s);
        }

        var campaignFaker = new Faker<CampaignEntity>()
            .RuleFor(c => c.Name, f => $"{f.Commerce.ProductName()}-{f.UniqueIndex}")
            .RuleFor(c => c.Club, club)
            .RuleFor(c => c.Season, f => f.PickRandom(seasons));

        var campaigns = campaignFaker.Generate(2);
        foreach (var c in campaigns)
        {
            club.Campaigns.Add(c);
        }

        var teamFaker = new Faker<TeamEntity>()
            .RuleFor(t => t.Name, f => $"{f.Company.CompanyName()}-{f.UniqueIndex}")
            .RuleFor(t => t.Club, club);

        var teams = teamFaker.Generate(2);
        foreach (var t in teams)
        {
            club.Teams.Add(t);
        }

        var playerFaker = new Faker<PlayerEntity>()
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Past(20)))
            .RuleFor(p => p.Club, club);

        var players = playerFaker.Generate(2);
        foreach (var p in players)
        {
            club.Players.Add(p);
        }
    }

    private static void SetCurrentUser(IServiceProvider services, long userId)
    {
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext = new DefaultHttpContext { User = principal };
    }

    [Fact]
    public async Task GetClubs_ShouldReturnOnlyUserClubs()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
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
