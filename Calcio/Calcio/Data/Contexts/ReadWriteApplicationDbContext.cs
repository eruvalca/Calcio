using Calcio.Data.Contexts.Base;
using Calcio.Data.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts;

public class ReadWriteApplicationDbContext(DbContextOptions<BaseDbContext> options,
        IHttpContextAccessor httpContextAccessor) : BaseDbContext(options, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClubEntity>()
            .HasQueryFilter(club => CurrentUserIdForFilters != null && club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<CampaignEntity>()
            .HasQueryFilter(campaign => CurrentUserIdForFilters != null && campaign.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<TeamEntity>()
            .HasQueryFilter(team => CurrentUserIdForFilters != null && team.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<PlayerEntity>()
            .HasQueryFilter(player => CurrentUserIdForFilters != null && player.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<NoteEntity>()
            .HasQueryFilter(note => CurrentUserIdForFilters != null && note.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<SeasonEntity>()
            .HasQueryFilter(season => CurrentUserIdForFilters != null && season.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<PlayerTagEntity>()
            .HasQueryFilter(playerTag => CurrentUserIdForFilters != null && playerTag.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<PlayerCampaignAssignmentEntity>()
            .HasQueryFilter(playerCampaignAssignment => CurrentUserIdForFilters != null && playerCampaignAssignment.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));

        builder.Entity<PlayerPhotoEntity>()
            .HasQueryFilter(playerPhoto => CurrentUserIdForFilters != null && playerPhoto.Club.CalcioUsers.Any(calcioUser => calcioUser.Id == CurrentUserIdForFilters));
    }
}
