using System.Security.Claims;
using System.Threading;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Calcio.IntegrationTests.Data.Contexts;

public abstract class BaseDbContextTests(CustomApplicationFactory factory) : IClassFixture<CustomApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomApplicationFactory Factory = factory;
    protected const long UserAId = 1;
    protected const long UserBId = 2;

    public virtual async ValueTask InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();

        // Set a user for seeding (auditing)
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        if (!await dbContext.Users.AnyAsync())
        {
            await SeedDataAsync(dbContext);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected static async Task SeedDataAsync(ReadWriteDbContext dbContext)
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

    protected static void GenerateClubEntities(ClubEntity club)
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

    protected static void SetCurrentUser(IServiceProvider services, long userId)
    {
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext = new DefaultHttpContext { User = principal };
    }

    protected static async Task SeedNotesForBothClubsAsync(ReadWriteDbContext context, CancellationToken cancellationToken)
    {
        var userPlayer = await GetPlayerForUserAsync(context, UserAId, cancellationToken);
        var otherPlayer = await GetPlayerForUserAsync(context, UserBId, cancellationToken);

        context.Notes.AddRange(
            new NoteEntity
            {
                PlayerId = userPlayer.PlayerId,
                ClubId = userPlayer.ClubId,
                Content = "User note",
                CreatedById = UserAId
            },
            new NoteEntity
            {
                PlayerId = otherPlayer.PlayerId,
                ClubId = otherPlayer.ClubId,
                Content = "Other user note",
                CreatedById = UserBId
            });

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    protected static async Task SeedPlayerTagsForBothClubsAsync(ReadWriteDbContext context, CancellationToken cancellationToken)
    {
        var userPlayer = await GetPlayerForUserAsync(context, UserAId, cancellationToken);
        var otherPlayer = await GetPlayerForUserAsync(context, UserBId, cancellationToken);

        context.PlayerTags.AddRange(
            new PlayerTagEntity
            {
                Name = "Speed",
                Color = "#FF0000",
                ClubId = userPlayer.ClubId,
                CreatedById = UserAId
            },
            new PlayerTagEntity
            {
                Name = "Strength",
                Color = "#00FF00",
                ClubId = otherPlayer.ClubId,
                CreatedById = UserBId
            });

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    protected static async Task SeedCampaignAssignmentsForBothClubsAsync(ReadWriteDbContext context, CancellationToken cancellationToken)
    {
        var userPlayer = await GetPlayerForUserAsync(context, UserAId, cancellationToken);
        var otherPlayer = await GetPlayerForUserAsync(context, UserBId, cancellationToken);
        var userCampaign = await GetCampaignForUserAsync(context, UserAId, cancellationToken);
        var otherCampaign = await GetCampaignForUserAsync(context, UserBId, cancellationToken);
        var userTeam = await GetTeamForUserAsync(context, UserAId, cancellationToken);
        var otherTeam = await GetTeamForUserAsync(context, UserBId, cancellationToken);

        context.PlayerCampaignAssignments.AddRange(
            new PlayerCampaignAssignmentEntity
            {
                PlayerId = userPlayer.PlayerId,
                CampaignId = userCampaign.CampaignId,
                TeamId = userTeam.TeamId,
                ClubId = userPlayer.ClubId,
                CreatedById = UserAId
            },
            new PlayerCampaignAssignmentEntity
            {
                PlayerId = otherPlayer.PlayerId,
                CampaignId = otherCampaign.CampaignId,
                TeamId = otherTeam.TeamId,
                ClubId = otherPlayer.ClubId,
                CreatedById = UserBId
            });

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    protected static async Task SeedPlayerPhotosForBothClubsAsync(ReadWriteDbContext context, CancellationToken cancellationToken)
    {
        var userPlayer = await GetPlayerForUserAsync(context, UserAId, cancellationToken);
        var otherPlayer = await GetPlayerForUserAsync(context, UserBId, cancellationToken);

        context.PlayerPhotos.AddRange(
            new PlayerPhotoEntity
            {
                PlayerId = userPlayer.PlayerId,
                ClubId = userPlayer.ClubId,
                OriginalBlobName = "user-original.jpg",
                CreatedById = UserAId
            },
            new PlayerPhotoEntity
            {
                PlayerId = otherPlayer.PlayerId,
                ClubId = otherPlayer.ClubId,
                OriginalBlobName = "other-original.jpg",
                CreatedById = UserBId
            });

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    protected static Task<PlayerEntity> GetPlayerForUserAsync(ReadWriteDbContext context, long userId, CancellationToken cancellationToken)
        => context.Players
            .IgnoreQueryFilters()
            .Include(p => p.Club)
            .ThenInclude(c => c.CalcioUsers)
            .FirstAsync(p => p.Club.CalcioUsers.Any(u => u.Id == userId), cancellationToken);

    protected static Task<CampaignEntity> GetCampaignForUserAsync(ReadWriteDbContext context, long userId, CancellationToken cancellationToken)
        => context.Campaigns
            .IgnoreQueryFilters()
            .Include(c => c.Club)
            .ThenInclude(club => club.CalcioUsers)
            .FirstAsync(c => c.Club.CalcioUsers.Any(u => u.Id == userId), cancellationToken);

    protected static Task<TeamEntity> GetTeamForUserAsync(ReadWriteDbContext context, long userId, CancellationToken cancellationToken)
        => context.Teams
            .IgnoreQueryFilters()
            .Include(t => t.Club)
            .ThenInclude(c => c.CalcioUsers)
            .FirstAsync(t => t.Club.CalcioUsers.Any(u => u.Id == userId), cancellationToken);
}
