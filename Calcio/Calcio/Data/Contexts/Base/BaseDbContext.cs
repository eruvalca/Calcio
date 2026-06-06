using System.Linq.Expressions;
using System.Security.Claims;

using Calcio.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts.Base;

/// <summary>
/// Represents the Base Db Context.
/// </summary>
public class BaseDbContext : IdentityDbContext<CalcioUserEntity, IdentityRole<long>, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="options">The options.</param>
    protected BaseDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        CurrentUserIdForFilters = ResolveCurrentUserId(httpContextAccessor);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    public BaseDbContext(DbContextOptions<BaseDbContext> options, IHttpContextAccessor httpContextAccessor)
        : this((DbContextOptions)options, httpContextAccessor)
    {
    }

    /// <summary>
    /// Gets the Current User Id For Filters.
    /// </summary>
    protected long CurrentUserIdForFilters { get; }

    /// <summary>
    /// Gets the Accessible Club Ids.
    /// </summary>
    protected IQueryable<long> AccessibleClubIds
        => Clubs
            .Where(club => club.CalcioUsers.Any(user => user.Id == CurrentUserIdForFilters))
            .Select(club => club.ClubId);

#pragma warning disable CA1822 // Mark members as static
    /// <summary>
    /// Gets the Clubs.
    /// </summary>
    public DbSet<ClubEntity> Clubs => Set<ClubEntity>();
    /// <summary>
    /// Gets the Campaigns.
    /// </summary>
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    /// <summary>
    /// Gets the Seasons.
    /// </summary>
    public DbSet<SeasonEntity> Seasons => Set<SeasonEntity>();
    /// <summary>
    /// Gets the Teams.
    /// </summary>
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    /// <summary>
    /// Gets the Players.
    /// </summary>
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    /// <summary>
    /// Gets the Notes.
    /// </summary>
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    /// <summary>
    /// Gets the Player Tags.
    /// </summary>
    public DbSet<PlayerTagEntity> PlayerTags => Set<PlayerTagEntity>();
    /// <summary>
    /// Gets the Player Campaign Assignments.
    /// </summary>
    public DbSet<PlayerCampaignAssignmentEntity> PlayerCampaignAssignments => Set<PlayerCampaignAssignmentEntity>();
    /// <summary>
    /// Gets the Club Join Requests.
    /// </summary>
    public DbSet<ClubJoinRequestEntity> ClubJoinRequests => Set<ClubJoinRequestEntity>();
    /// <summary>
    /// Gets the Player Photos.
    /// </summary>
    public DbSet<PlayerPhotoEntity> PlayerPhotos => Set<PlayerPhotoEntity>();
    /// <summary>
    /// Gets the Calcio User Photos.
    /// </summary>
    public DbSet<CalcioUserPhotoEntity> CalcioUserPhotos => Set<CalcioUserPhotoEntity>();
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// Executes the Configure Conventions operation.
    /// </summary>
    /// <param name="configurationBuilder">The configuration Builder.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => base.ConfigureConventions(configurationBuilder);

    /// <summary>
    /// Executes the On Model Creating operation.
    /// </summary>
    /// <param name="modelBuilder">The model Builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDbContext).Assembly);
    }

    protected Expression<Func<TEntity, bool>> IsOwnedByAccessibleClub<TEntity>(string clubIdPropertyName)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clubIdPropertyName);
        return entity => AccessibleClubIds.Contains(EF.Property<long>(entity, clubIdPropertyName));
    }

    /// <summary>
    /// Executes the Resolve Current User Id operation.
    /// </summary>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    /// <returns>The operation result.</returns>
    private static long ResolveCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var rawId = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(rawId, out var userId) ? userId : 0;
    }
}
