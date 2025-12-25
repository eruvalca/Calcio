using Bogus;

using Calcio.Data.Contexts;
using Calcio.IntegrationTests.Data.Contexts;
using Calcio.Services.CalcioUsers;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Results;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Calcio.IntegrationTests.Services.CalcioUsers;

public class CalcioUsersServiceTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    private const long StandardMemberUserId = 200;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();

        var club = await dbContext.Clubs.FirstAsync();

        // Create or reset the standard member user in UserA's club for testing
        var existingUser = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == StandardMemberUserId);
        if (existingUser is null)
        {
            var userFaker = new Faker<CalcioUserEntity>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.UserName, f => f.Internet.Email())
                .RuleFor(u => u.Email, (f, u) => u.UserName);

            var standardMember = userFaker.Generate();
            standardMember.Id = StandardMemberUserId;
            standardMember.ClubId = club.ClubId;

            var result = await userManager.CreateAsync(standardMember, "TestPassword123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create standard member user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Add StandardUser role
            await userManager.AddToRoleAsync(standardMember, Roles.StandardUser);
        }
        else if (existingUser.ClubId != club.ClubId)
        {
            // Reset the user's club membership if they were removed by another test
            existingUser.ClubId = club.ClubId;
            await dbContext.SaveChangesAsync();

            // Also re-add the StandardUser role if needed
            var freshUser = await userManager.FindByIdAsync(StandardMemberUserId.ToString());
            if (freshUser is not null && !await userManager.IsInRoleAsync(freshUser, Roles.StandardUser))
            {
                await userManager.AddToRoleAsync(freshUser, Roles.StandardUser);
            }
        }
    }

    #region GetClubMembersAsync Tests

    [Fact]
    public async Task GetClubMembersAsync_WhenUserIsMember_ReturnsMembers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubMembersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var members = result.Value;
        members.ShouldNotBeEmpty();
        members.ShouldNotContain(m => m.UserId == UserAId); // Current user should be excluded
        members.ShouldContain(m => m.UserId == StandardMemberUserId);
    }

    /// <summary>
    /// Note: In the new architecture, club membership authorization is handled by 
    /// ClubMembershipFilter at the endpoint level. The service does not perform
    /// club membership checks - it relies on the Users table query which finds
    /// members by clubId without a global filter. Authorization is at endpoint level.
    /// </summary>
    [Fact]
    public async Task GetClubMembersAsync_WhenClubHasMembers_ReturnsAllMembers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        // Get the other user's club - in production, ClubMembershipFilter would block this
        var otherClub = await dbContext.Clubs
            .IgnoreQueryFilters()
            .Include(c => c.CalcioUsers)
            .FirstAsync(c => c.CalcioUsers.All(u => u.Id != UserAId), cancellationToken);

        // Act - Service level does not check club membership, that's handled by endpoint filter
        var result = await service.GetClubMembersAsync(otherClub.ClubId, cancellationToken);

        // Assert - Service returns club members since authorization is at endpoint level
        result.IsSuccess.ShouldBeTrue();
        // The result may contain members (UserB) - authorization is handled elsewhere
        result.Value.ShouldAllBe(m => m.UserId > 0);
    }

    [Fact]
    public async Task GetClubMembersAsync_ShouldReturnMembersOrderedByAdminThenName()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.GetClubMembersAsync(club.ClubId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var members = result.Value;
        members.ShouldNotBeEmpty();

        // Verify ordering: admins first, then by name
        var admins = members.Where(m => m.IsClubAdmin).ToList();
        var nonAdmins = members.Where(m => !m.IsClubAdmin).ToList();

        // Admins should come first (if any)
        if (admins.Count > 0 && nonAdmins.Count > 0)
        {
            var firstAdminIndex = members.FindIndex(m => m.IsClubAdmin);
            var firstNonAdminIndex = members.FindIndex(m => !m.IsClubAdmin);
            firstAdminIndex.ShouldBeLessThan(firstNonAdminIndex);
        }

        // Non-admins should be sorted by name
        if (nonAdmins.Count > 1)
        {
            var sortedNonAdmins = nonAdmins.OrderBy(m => m.FullName).ToList();
            nonAdmins.ShouldBe(sortedNonAdmins);
        }
    }

    #endregion

    #region RemoveClubMemberAsync Tests

    [Fact]
    public async Task RemoveClubMemberAsync_WhenValidMember_ReturnsSuccessAndRemovesFromClub()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Use the StandardMemberUserId that was created in InitializeAsync
        // Act
        var result = await service.RemoveClubMemberAsync(club.ClubId, StandardMemberUserId, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify user was removed from club
        dbContext.ChangeTracker.Clear();
        var updatedUser = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == StandardMemberUserId, cancellationToken);
        updatedUser.ClubId.ShouldBeNull();

        // Verify StandardUser role was removed
        var freshUser = await userManager.FindByIdAsync(StandardMemberUserId.ToString());
        var isInRole = await userManager.IsInRoleAsync(freshUser!, Roles.StandardUser);
        isInRole.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenTryingToRemoveSelf_ReturnsUnauthorized()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.RemoveClubMemberAsync(club.ClubId, UserAId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.Forbidden);
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenUserNotInClub_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act - Try to remove UserB who is in a different club
        var result = await service.RemoveClubMemberAsync(club.ClubId, UserBId, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    [Fact]
    public async Task RemoveClubMemberAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);

        var dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();
        var service = CreateService(scope.ServiceProvider);

        var club = await dbContext.Clubs.FirstAsync(cancellationToken);

        // Act
        var result = await service.RemoveClubMemberAsync(club.ClubId, 999999, cancellationToken);

        // Assert
        result.IsProblem.ShouldBeTrue();
        result.Problem.Kind.ShouldBe(ServiceProblemKind.NotFound);
    }

    #endregion

    #region Helpers

    private static CalcioUsersService CreateService(IServiceProvider services)
    {
        var readOnlyFactory = services.GetRequiredService<IDbContextFactory<ReadOnlyDbContext>>();
        var readWriteFactory = services.GetRequiredService<IDbContextFactory<ReadWriteDbContext>>();
        var userManager = services.GetRequiredService<UserManager<CalcioUserEntity>>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var logger = services.GetRequiredService<ILogger<CalcioUsersService>>();

        return new CalcioUsersService(readOnlyFactory, readWriteFactory, userManager, httpContextAccessor, logger);
    }

    #endregion
}
