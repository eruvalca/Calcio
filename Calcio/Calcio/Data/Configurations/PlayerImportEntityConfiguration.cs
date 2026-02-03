using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class PlayerImportEntityConfiguration : IEntityTypeConfiguration<PlayerImportEntity>
{
    public void Configure(EntityTypeBuilder<PlayerImportEntity> builder)
    {
        builder.HasKey(e => e.ImportId);
        builder.Property(e => e.ImportId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Rows)
            .WithOne(r => r.Import)
            .HasForeignKey(r => r.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.FileName)
            .HasMaxLength(255);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);
    }
}
