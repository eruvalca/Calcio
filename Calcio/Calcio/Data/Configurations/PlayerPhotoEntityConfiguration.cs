using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public sealed class PlayerPhotoEntityConfiguration : IEntityTypeConfiguration<PlayerPhotoEntity>
{
    public void Configure(EntityTypeBuilder<PlayerPhotoEntity> builder)
    {
        builder.HasKey(e => e.PlayerPhotoId);
        builder.Property(e => e.PlayerPhotoId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Player)
            .WithMany(p => p.Photos)
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Club)
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
