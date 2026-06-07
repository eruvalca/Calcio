using Calcio.Data.Contexts;
using Calcio.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.Integration.Tests.Data.Contexts;

/// <summary>
/// Contains integration tests for read write db context behavior.
/// </summary>
/// <param name="factory">Provides dependencies used to build the integration test host.</param>
public class ReadWriteDbContextTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    /// <summary>
    /// Verifies that get clubs should return only user clubs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get seasons should return only user seasons.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get campaigns should return only user campaigns.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get teams should return only user teams.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get players should return only user players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get notes should return only user notes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get player tags should return only user tags.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get player campaign assignments should return only user assignments.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that get player photos should return only user photos.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that query without authenticated user should return empty results.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that delete club with campaigns should cascade dependents.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteClub_WithCampaigns_ShouldCascadeDependents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeNote: false, includePhoto: false, includeAssignment: false, token);

            var campaignCount = await context.Campaigns
                .IgnoreQueryFilters()
                .CountAsync(c => c.ClubId == graph.ClubId, token);
            campaignCount.ShouldBeGreaterThan(0);

            var club = await context.Clubs
                .IgnoreQueryFilters()
                .FirstAsync(c => c.ClubId == graph.ClubId, token);

            context.Clubs.Remove(club);

            await context.SaveChangesAsync(token);

            (await context.Campaigns.IgnoreQueryFilters().AnyAsync(c => c.ClubId == graph.ClubId, token)).ShouldBeFalse();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that delete club with notes should cascade dependents.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteClub_WithNotes_ShouldCascadeDependents()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeNote: true, includePhoto: false, includeAssignment: false, token);

            var club = await context.Clubs
                .IgnoreQueryFilters()
                .FirstAsync(c => c.ClubId == graph.ClubId, token);

            context.Clubs.Remove(club);
            await context.SaveChangesAsync(token);

            (await context.Notes.IgnoreQueryFilters().AnyAsync(n => n.ClubId == graph.ClubId, token)).ShouldBeFalse();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that delete club should cascade entire graph.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteClub_ShouldCascadeEntireGraph()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeNote: true, includePhoto: true, includeAssignment: true, token);

            var club = await context.Clubs
                .IgnoreQueryFilters()
                .FirstAsync(c => c.ClubId == graph.ClubId, token);

            context.Clubs.Remove(club);
            await context.SaveChangesAsync(token);

            (await context.Campaigns.IgnoreQueryFilters().AnyAsync(c => c.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.Players.IgnoreQueryFilters().AnyAsync(p => p.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.Teams.IgnoreQueryFilters().AnyAsync(t => t.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.Seasons.IgnoreQueryFilters().AnyAsync(s => s.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.PlayerTags.IgnoreQueryFilters().AnyAsync(pt => pt.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.Notes.IgnoreQueryFilters().AnyAsync(n => n.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.PlayerPhotos.IgnoreQueryFilters().AnyAsync(p => p.ClubId == graph.ClubId, token)).ShouldBeFalse();
            (await context.PlayerCampaignAssignments.IgnoreQueryFilters().AnyAsync(a => a.ClubId == graph.ClubId, token)).ShouldBeFalse();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that delete player should cascade dependent entities.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeletePlayer_ShouldCascadeDependentEntities()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeNote: true, includePhoto: true, includeAssignment: true, token);

            var player = await context.Players
                .IgnoreQueryFilters()
                .Include(p => p.Notes)
                .Include(p => p.Photos)
                .Include(p => p.CampaignAssignments)
                .FirstAsync(p => p.PlayerId == graph.PlayerId, token);

            graph.AssignmentId.ShouldNotBeNull();
            graph.NoteId.ShouldNotBeNull();
            graph.PhotoId.ShouldNotBeNull();

            context.Players.Remove(player);
            await context.SaveChangesAsync(token);

            (await context.Notes.IgnoreQueryFilters().AnyAsync(n => n.PlayerId == graph.PlayerId, token)).ShouldBeFalse();
            (await context.PlayerPhotos.IgnoreQueryFilters().AnyAsync(p => p.PlayerId == graph.PlayerId, token)).ShouldBeFalse();
            (await context.PlayerCampaignAssignments.IgnoreQueryFilters().AnyAsync(a => a.PlayerId == graph.PlayerId, token)).ShouldBeFalse();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that delete campaign should cascade assignments.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteCampaign_ShouldCascadeAssignments()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeAssignment: true, cancellationToken: token);
            graph.AssignmentId.ShouldNotBeNull();

            var campaign = await context.Campaigns
                .IgnoreQueryFilters()
                .FirstAsync(c => c.CampaignId == graph.CampaignId, token);

            context.Campaigns.Remove(campaign);
            await context.SaveChangesAsync(token);

            (await context.PlayerCampaignAssignments.IgnoreQueryFilters().AnyAsync(a => a.CampaignId == graph.CampaignId, token)).ShouldBeFalse();
            (await context.Players.IgnoreQueryFilters().AnyAsync(p => p.PlayerId == graph.PlayerId, token)).ShouldBeTrue();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that delete team should cascade assignments.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteTeam_ShouldCascadeAssignments()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var token = TestContext.Current.CancellationToken;

        ClubGraphIds? graph = null;
        try
        {
            graph = await CreateClubGraphAsync(context, includeAssignment: true, cancellationToken: token);
            graph.AssignmentId.ShouldNotBeNull();

            var team = await context.Teams
                .IgnoreQueryFilters()
                .Include(t => t.PlayerAssignments)
                .FirstAsync(t => t.TeamId == graph.TeamId, token);

            team.PlayerAssignments.ShouldNotBeEmpty();

            context.Teams.Remove(team);
            await context.SaveChangesAsync(token);

            (await context.PlayerCampaignAssignments.IgnoreQueryFilters().AnyAsync(a => a.TeamId == graph.TeamId, token)).ShouldBeFalse();
        }
        finally
        {
            if (graph is ClubGraphIds createdGraph)
            {
                await CleanupClubGraphAsync(context, createdGraph, token);
            }
        }
    }

    /// <summary>
    /// Verifies that get club join requests as club member should return requests for club.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetClubJoinRequests_AsClubMember_ShouldReturnRequestsForClub()
    {
        var token = TestContext.Current.CancellationToken;

        using var seedScope = CreateContextScope(UserBId, out var seedContext);
        var userAClub = await GetClubForUserAsync(seedContext, UserAId, token);
        var joinRequestId = await CreateJoinRequestAsync(seedContext, userAClub.ClubId, UserBId, token);

        try
        {
            using var queryScope = CreateContextScope(UserAId, out var queryContext);

            var requests = await queryContext.ClubJoinRequests
                .Include(r => r.Club)
                .ThenInclude(c => c.CalcioUsers)
                .ToListAsync(token);

            var request = requests.ShouldHaveSingleItem();
            request.ClubJoinRequestId.ShouldBe(joinRequestId);
            request.Club.CalcioUsers.ShouldContain(u => u.Id == UserAId);
        }
        finally
        {
            using var cleanupScope = CreateContextScope(UserAId, out var cleanupContext);
            await RemoveJoinRequestAsync(cleanupContext, joinRequestId, token);
        }
    }

    /// <summary>
    /// Verifies that get club join requests as requesting user should return own requests.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetClubJoinRequests_AsRequestingUser_ShouldReturnOwnRequests()
    {
        var token = TestContext.Current.CancellationToken;

        using var seedScope = CreateContextScope(UserBId, out var seedContext);
        var userAClub = await GetClubForUserAsync(seedContext, UserAId, token);
        var joinRequestId = await CreateJoinRequestAsync(seedContext, userAClub.ClubId, UserBId, token);

        try
        {
            using var queryScope = CreateContextScope(UserBId, out var queryContext);

            var requests = await queryContext.ClubJoinRequests
                .AsNoTracking()
                .ToListAsync(token);

            var request = requests.ShouldHaveSingleItem();
            request.RequestingUserId.ShouldBe(UserBId);
            request.ClubId.ShouldBe(userAClub.ClubId);
        }
        finally
        {
            using var cleanupScope = CreateContextScope(UserAId, out var cleanupContext);
            await RemoveJoinRequestAsync(cleanupContext, joinRequestId, token);
        }
    }

    /// <summary>
    /// Verifies that get club join requests as unrelated user should not return requests.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetClubJoinRequests_AsUnrelatedUser_ShouldNotReturnRequests()
    {
        var token = TestContext.Current.CancellationToken;

        using var seedScope = CreateContextScope(UserBId, out var seedContext);
        var userBClub = await GetClubForUserAsync(seedContext, UserBId, token);
        var joinRequestId = await CreateJoinRequestAsync(seedContext, userBClub.ClubId, UserBId, token);

        try
        {
            using var queryScope = CreateContextScope(UserAId, out var queryContext);

            var requests = await queryContext.ClubJoinRequests
                .AsNoTracking()
                .ToListAsync(token);

            requests.ShouldBeEmpty();
        }
        finally
        {
            using var cleanupScope = CreateContextScope(UserAId, out var cleanupContext);
            await RemoveJoinRequestAsync(cleanupContext, joinRequestId, token);
        }
    }

    /// <summary>
    /// Finds a club that contains the specified user membership.
    /// </summary>
    /// <param name="context">Specifies the context value for this scenario.</param>
    /// <param name="userId">Specifies the user id value for this scenario.</param>
    /// <param name="token">Specifies the token value for this scenario.</param>
    /// <returns>A task that represents the asynchronous operation result.</returns>
    private static Task<ClubEntity> GetClubForUserAsync(ReadWriteDbContext context, long userId, CancellationToken token)
        => context.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.Any(u => u.Id == userId), token);

    /// <summary>
    /// Creates a join request entity used by club join request filter tests.
    /// </summary>
    /// <param name="context">Specifies the context value for this scenario.</param>
    /// <param name="clubId">Specifies the club id value for this scenario.</param>
    /// <param name="requestingUserId">Specifies the requesting user id value for this scenario.</param>
    /// <param name="token">Specifies the token value for this scenario.</param>
    /// <returns>A task that represents the asynchronous operation result.</returns>
    private static async Task<long> CreateJoinRequestAsync(ReadWriteDbContext context, long clubId, long requestingUserId, CancellationToken token)
    {
        var joinRequest = new ClubJoinRequestEntity
        {
            ClubId = clubId,
            RequestingUserId = requestingUserId,
            CreatedById = requestingUserId
        };

        await context.ClubJoinRequests.AddAsync(joinRequest, token);
        await context.SaveChangesAsync(token);
        var requestId = joinRequest.ClubJoinRequestId;
        context.ChangeTracker.Clear();
        return requestId;
    }

    /// <summary>
    /// Removes a join request entity created for club join request filter tests.
    /// </summary>
    /// <param name="context">Specifies the context value for this scenario.</param>
    /// <param name="joinRequestId">Specifies the join request id value for this scenario.</param>
    /// <param name="token">Specifies the token value for this scenario.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task RemoveJoinRequestAsync(ReadWriteDbContext context, long joinRequestId, CancellationToken token)
    {
        var existing = await context.ClubJoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.ClubJoinRequestId == joinRequestId, token);

        if (existing is null)
        {
            return;
        }

        context.ClubJoinRequests.Remove(existing);
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Creates a scoped context configured for the specified authenticated user.
    /// </summary>
    /// <param name="userId">Specifies the user id value for this scenario.</param>
    /// <param name="context">Specifies the context value for this scenario.</param>
    /// <returns>The i service scope produced by the operation.</returns>
    private IServiceScope CreateContextScope(long userId, out ReadWriteDbContext context)
    {
        var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, userId);
        context = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        return scope;
    }
}
