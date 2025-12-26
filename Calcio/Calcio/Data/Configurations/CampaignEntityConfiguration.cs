using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class CampaignEntityConfiguration : IEntityTypeConfiguration<CampaignEntity>
{
    public void Configure(EntityTypeBuilder<CampaignEntity> builder)
    {
        builder.HasKey(e => e.CampaignId);
        builder.Property(e => e.CampaignId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.Campaigns)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Season)
            .WithMany(s => s.Campaigns)
            .HasForeignKey(e => e.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SeasonId);
    }
}
