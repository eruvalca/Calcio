using Calcio.Shared.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

public class ClubJoinRequestEntityConfiguration : IEntityTypeConfiguration<ClubJoinRequestEntity>
{
    public void Configure(EntityTypeBuilder<ClubJoinRequestEntity> builder)
    {
        builder.HasKey(e => e.ClubJoinRequestId);
        builder.Property(e => e.ClubJoinRequestId)
            .ValueGeneratedOnAdd();

        builder
            .HasOne(e => e.Club)
            .WithMany(c => c.JoinRequests)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.RequestingUser)
            .WithOne(u => u.SentJoinRequest)
            .HasForeignKey<ClubJoinRequestEntity>(e => e.RequestingUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
