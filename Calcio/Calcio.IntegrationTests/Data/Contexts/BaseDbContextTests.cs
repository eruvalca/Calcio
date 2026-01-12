using System.Security.Claims;

using Bogus;

using Calcio.Data.Contexts;
using Calcio.Shared.Entities;

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
            .RuleFor(u => u.UserName, f => f.Internet.Email());

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
            .RuleFor(t => t.GraduationYear, f => f.Date.Future(10).Year)
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
            .RuleFor(p => p.GraduationYear, f => f.Date.Future(10).Year)
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

    protected sealed record ClubGraphIds(
        long ClubId,
        long PlayerId,
        long CampaignId,
        long TeamId,
        long SeasonId,
        long PlayerTagId,
        long? NoteId,
        long? PhotoId,
        long? AssignmentId);

    protected static async Task<ClubGraphIds> CreateClubGraphAsync(
        ReadWriteDbContext context,
        bool includeNote = true,
        bool includePhoto = true,
        bool includeAssignment = true,
        CancellationToken cancellationToken = default)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");

        var club = new ClubEntity
        {
            Name = $"Cascade Club {uniqueSuffix}",
            City = "Cascade City",
            State = "CC",
            CreatedById = UserAId
        };

        var season = new SeasonEntity
        {
            Name = $"Season {uniqueSuffix}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Club = club,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        var campaign = new CampaignEntity
        {
            Name = $"Campaign {uniqueSuffix}",
            Club = club,
            Season = season,
            ClubId = club.ClubId,
            SeasonId = season.SeasonId,
            CreatedById = UserAId
        };

        var team = new TeamEntity
        {
            Name = $"Team {uniqueSuffix}",
            GraduationYear = DateTime.Today.Year + 5,
            Club = club,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        var player = new PlayerEntity
        {
            FirstName = "Cascade",
            LastName = uniqueSuffix,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-12)),
            GraduationYear = DateTime.Today.Year + 6,
            Club = club,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        var tag = new PlayerTagEntity
        {
            Name = $"Tag {uniqueSuffix}",
            Color = "#123456",
            Club = club,
            ClubId = club.ClubId,
            CreatedById = UserAId
        };

        NoteEntity? note = null;
        if (includeNote)
        {
            note = new NoteEntity
            {
                Player = player,
                Club = club,
                Content = "Restricted club note",
                PlayerId = player.PlayerId,
                ClubId = club.ClubId,
                CreatedById = UserAId
            };
        }

        PlayerPhotoEntity? photo = null;
        if (includePhoto)
        {
            photo = new PlayerPhotoEntity
            {
                Player = player,
                Club = club,
                OriginalBlobName = $"photo-{uniqueSuffix}.jpg",
                PlayerId = player.PlayerId,
                ClubId = club.ClubId,
                CreatedById = UserAId
            };
        }

        PlayerCampaignAssignmentEntity? assignment = null;
        if (includeAssignment)
        {
            assignment = new PlayerCampaignAssignmentEntity
            {
                Player = player,
                Campaign = campaign,
                Team = team,
                Club = club,
                PlayerId = player.PlayerId,
                CampaignId = campaign.CampaignId,
                TeamId = team.TeamId,
                ClubId = club.ClubId,
                CreatedById = UserAId
            };
        }

        await context.AddAsync(club, cancellationToken);
        await context.AddAsync(season, cancellationToken);
        await context.AddAsync(campaign, cancellationToken);
        await context.AddAsync(team, cancellationToken);
        await context.AddAsync(player, cancellationToken);
        await context.AddAsync(tag, cancellationToken);

        if (note is not null)
        {
            await context.AddAsync(note, cancellationToken);
        }

        if (photo is not null)
        {
            await context.AddAsync(photo, cancellationToken);
        }

        if (assignment is not null)
        {
            await context.AddAsync(assignment, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        return new ClubGraphIds(
            club.ClubId,
            player.PlayerId,
            campaign.CampaignId,
            team.TeamId,
            season.SeasonId,
            tag.PlayerTagId,
            note?.NoteId,
            photo?.PlayerPhotoId,
            assignment?.PlayerCampaignAssignmentId);
    }

    protected static async Task CleanupClubGraphAsync(ReadWriteDbContext context, ClubGraphIds graph, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();

        var assignments = await context.PlayerCampaignAssignments
            .IgnoreQueryFilters()
            .Where(a => a.ClubId == graph.ClubId)
            .ToListAsync(cancellationToken);
        if (assignments.Count > 0)
        {
            context.PlayerCampaignAssignments.RemoveRange(assignments);
        }

        var photos = await context.PlayerPhotos
            .IgnoreQueryFilters()
            .Where(p => p.ClubId == graph.ClubId)
            .ToListAsync(cancellationToken);
        if (photos.Count > 0)
        {
            context.PlayerPhotos.RemoveRange(photos);
        }

        var notes = await context.Notes
            .IgnoreQueryFilters()
            .Where(n => n.ClubId == graph.ClubId)
            .ToListAsync(cancellationToken);
        if (notes.Count > 0)
        {
            context.Notes.RemoveRange(notes);
        }

        var campaigns = await context.Campaigns
            .IgnoreQueryFilters()
            .Where(c => c.ClubId == graph.ClubId)
            .ToListAsync(cancellationToken);
        if (campaigns.Count > 0)
        {
            context.Campaigns.RemoveRange(campaigns);
        }

        if (assignments.Count > 0 || photos.Count > 0 || notes.Count > 0 || campaigns.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        var club = await context.Clubs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ClubId == graph.ClubId, cancellationToken);
        if (club is null)
        {
            return;
        }

        context.Clubs.Remove(club);
        await context.SaveChangesAsync(cancellationToken);
    }
}
