using Calcio.Data.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class SeasonEntityConfiguration : IEntityTypeConfiguration<SeasonEntity>
{
    public void Configure(EntityTypeBuilder<SeasonEntity> builder)
    {
        builder.HasKey(e => e.SeasonId);
        builder.Property(e => e.SeasonId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.Seasons)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Campaigns)
            .WithOne(c => c.Season!)
            .HasForeignKey(c => c.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ClubId, e.Name }).IsUnique();
    }
}
