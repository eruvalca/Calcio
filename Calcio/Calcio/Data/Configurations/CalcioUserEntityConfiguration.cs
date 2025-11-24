using Calcio.Shared.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class CalcioUserEntityConfiguration : IEntityTypeConfiguration<CalcioUserEntity>
{
    public void Configure(EntityTypeBuilder<CalcioUserEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.SentJoinRequest)
            .WithOne(r => r.RequestingUser)
            .HasForeignKey<ClubJoinRequestEntity>(r => r.RequestingUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
