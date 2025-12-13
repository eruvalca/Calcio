using Calcio.Shared.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public sealed class CalcioUserPhotoEntityConfiguration : IEntityTypeConfiguration<CalcioUserPhotoEntity>
{
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
