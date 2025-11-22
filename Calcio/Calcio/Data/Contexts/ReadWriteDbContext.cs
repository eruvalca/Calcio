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
            .HasQueryFilter(club => club.CalcioUsers.Any(u => u.Id == CurrentUserIdForFilters));

        builder.Entity<CampaignEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<CampaignEntity>(nameof(CampaignEntity.ClubId)));

        builder.Entity<TeamEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<TeamEntity>(nameof(TeamEntity.ClubId)));

        builder.Entity<PlayerEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<PlayerEntity>(nameof(PlayerEntity.ClubId)));

        builder.Entity<NoteEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<NoteEntity>(nameof(NoteEntity.ClubId)));

        builder.Entity<SeasonEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<SeasonEntity>(nameof(SeasonEntity.ClubId)));

        builder.Entity<PlayerTagEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<PlayerTagEntity>(nameof(PlayerTagEntity.ClubId)));

        builder.Entity<PlayerCampaignAssignmentEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<PlayerCampaignAssignmentEntity>(nameof(PlayerCampaignAssignmentEntity.ClubId)));

        builder.Entity<PlayerPhotoEntity>()
            .HasQueryFilter(IsOwnedByAccessibleClub<PlayerPhotoEntity>(nameof(PlayerPhotoEntity.ClubId)));

        builder.Entity<ClubJoinRequestEntity>()
            .HasQueryFilter(request
                => request.RequestingUserId == CurrentUserIdForFilters
                    || AccessibleClubIds.Contains(request.ClubId));
    }
}
