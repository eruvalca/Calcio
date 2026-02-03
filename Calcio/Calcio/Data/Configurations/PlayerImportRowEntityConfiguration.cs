using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class PlayerImportRowEntityConfiguration : IEntityTypeConfiguration<PlayerImportRowEntity>
{
    public void Configure(EntityTypeBuilder<PlayerImportRowEntity> builder)
    {
        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Import)
            .WithMany(i => i.Rows)
            .HasForeignKey(e => e.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.CreatedPlayer)
            .WithMany()
            .HasForeignKey(e => e.CreatedPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.RawData)
            .HasMaxLength(4000);

        builder.HasIndex(e => e.ImportId);
    }
}
