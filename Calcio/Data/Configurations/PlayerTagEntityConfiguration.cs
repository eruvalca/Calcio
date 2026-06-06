using Calcio.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

/// <summary>
/// Configures EF Core mapping for Player Tag Entity Configuration.
/// </summary>
public class PlayerTagEntityConfiguration : IEntityTypeConfiguration<PlayerTagEntity>
{
    /// <summary>
    /// Executes the Configure operation.
    /// </summary>
    /// <param name="builder">The builder.</param>
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
