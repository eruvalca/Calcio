using Calcio.Shared.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class TeamEntityConfiguration : IEntityTypeConfiguration<TeamEntity>
{
    public void Configure(EntityTypeBuilder<TeamEntity> builder)
    {
        builder.HasKey(e => e.TeamId);
        builder.Property(e => e.TeamId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.Teams)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
