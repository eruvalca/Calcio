using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts.Base;

public class BaseDbContext(DbContextOptions<BaseDbContext> options,
 IHttpContextAccessor httpContextAccessor) : IdentityDbContext<CalcioUserEntity, IdentityRole<long>, long>(options)
{
    // For global query filters: prefer a non-throwing path that yields an empty set when no user is present
    protected bool HasUserForFilters => httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is { Length: > 0 };
    protected long? CurrentUserIdForFilters => httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is string id && id.Length > 0
    ? long.Parse(id)
    : null;

    public long GetCurrentCalcioUserId()
    {
        var currentCalcioUserIdPrimitive = (httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
        ?? throw new InvalidOperationException("UserId was null when trying to create or update an entity.");
        return long.Parse(currentCalcioUserIdPrimitive);
    }

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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDbContext).Assembly);
    }
}
