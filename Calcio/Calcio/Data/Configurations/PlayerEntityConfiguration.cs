using Calcio.Shared.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.HasKey(e => e.PlayerId);
        builder.Property(e => e.PlayerId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.Players)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(p => p.Notes)
            .WithOne(n => n.Player)
            .HasForeignKey(n => n.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(p => p.Tags)
            .WithMany()
            .UsingEntity(
                right => right.HasOne(typeof(PlayerTagEntity)).WithMany().OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne(typeof(PlayerEntity)).WithMany().OnDelete(DeleteBehavior.ClientCascade)
            );
    }
}
