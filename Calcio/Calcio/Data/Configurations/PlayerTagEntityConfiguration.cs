using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class PlayerTagEntityConfiguration : IEntityTypeConfiguration<PlayerTagEntity>
{
    public void Configure(EntityTypeBuilder<PlayerTagEntity> builder)
    {
        builder.HasKey(e => e.PlayerTagId);
        builder.Property(e => e.PlayerTagId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.PlayerTags)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
