using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class PlayerCampaignAssignmentEntityConfiguration : IEntityTypeConfiguration<PlayerCampaignAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<PlayerCampaignAssignmentEntity> builder)
    {
        builder.HasKey(e => e.PlayerCampaignAssignmentId);
        builder.Property(e => e.PlayerCampaignAssignmentId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Player)
            .WithMany(p => p.CampaignAssignments)
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Campaign)
            .WithMany(c => c.PlayerAssignments)
            .HasForeignKey(e => e.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Team)
            .WithMany(t => t.PlayerAssignments)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Club)
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
