using Calcio.Data.Contexts.Base;
using Calcio.Data.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts;

public class ReadWriteDbContext(DbContextOptions<ReadWriteDbContext> options,
        IHttpContextAccessor httpContextAccessor) : BaseDbContext(options, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClubEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<ClubEntity>(nameof(ClubEntity.ClubId)));

        builder.Entity<CampaignEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<CampaignEntity>(nameof(CampaignEntity.ClubId)));

        builder.Entity<TeamEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<TeamEntity>(nameof(TeamEntity.ClubId)));

        builder.Entity<PlayerEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<PlayerEntity>(nameof(PlayerEntity.ClubId)));

        builder.Entity<NoteEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<NoteEntity>(nameof(NoteEntity.ClubId)));

        builder.Entity<SeasonEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<SeasonEntity>(nameof(SeasonEntity.ClubId)));

        builder.Entity<PlayerTagEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<PlayerTagEntity>(nameof(PlayerTagEntity.ClubId)));

        builder.Entity<PlayerCampaignAssignmentEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<PlayerCampaignAssignmentEntity>(nameof(PlayerCampaignAssignmentEntity.ClubId)));

        builder.Entity<PlayerPhotoEntity>()
            .HasQueryFilter(IsOwnedByCurrentUser<PlayerPhotoEntity>(nameof(PlayerPhotoEntity.ClubId)));
    }
}
