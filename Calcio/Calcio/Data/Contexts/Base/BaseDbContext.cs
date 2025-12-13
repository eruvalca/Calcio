using System.Linq.Expressions;
using System.Security.Claims;

using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts.Base;

public class BaseDbContext : IdentityDbContext<CalcioUserEntity, IdentityRole<long>, long>
{
    protected BaseDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        CurrentUserIdForFilters = ResolveCurrentUserId(httpContextAccessor);
    }

    public BaseDbContext(DbContextOptions<BaseDbContext> options, IHttpContextAccessor httpContextAccessor)
        : this((DbContextOptions)options, httpContextAccessor)
    {
    }

    protected long CurrentUserIdForFilters { get; }

    protected IQueryable<long> AccessibleClubIds
        => Clubs
            .Where(club => club.CalcioUsers.Any(user => user.Id == CurrentUserIdForFilters))
            .Select(club => club.ClubId);

#pragma warning disable CA1822 // Mark members as static
    public DbSet<ClubEntity> Clubs => Set<ClubEntity>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<SeasonEntity> Seasons => Set<SeasonEntity>();
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<PlayerTagEntity> PlayerTags => Set<PlayerTagEntity>();
    public DbSet<PlayerCampaignAssignmentEntity> PlayerCampaignAssignments => Set<PlayerCampaignAssignmentEntity>();
    public DbSet<ClubJoinRequestEntity> ClubJoinRequests => Set<ClubJoinRequestEntity>();
    public DbSet<PlayerPhotoEntity> PlayerPhotos => Set<PlayerPhotoEntity>();
    public DbSet<CalcioUserPhotoEntity> CalcioUserPhotos => Set<CalcioUserPhotoEntity>();
#pragma warning restore CA1822 // Mark members as static

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => base.ConfigureConventions(configurationBuilder);

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

    private static long ResolveCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var rawId = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(rawId, out var userId) ? userId : 0;
    }
}
