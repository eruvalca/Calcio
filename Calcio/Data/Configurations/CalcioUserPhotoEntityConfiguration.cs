using Calcio.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

/// <summary>
/// Configures EF Core mapping for Calcio User Photo Entity Configuration.
/// </summary>
public sealed class CalcioUserPhotoEntityConfiguration : IEntityTypeConfiguration<CalcioUserPhotoEntity>
{
    /// <summary>
    /// Executes the Configure operation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<CalcioUserPhotoEntity> builder)
    {
        builder.HasKey(e => e.CalcioUserPhotoId);
        builder.Property(e => e.CalcioUserPhotoId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.CalcioUser)
            .WithMany(u => u.Photos)
            .HasForeignKey(e => e.CalcioUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
