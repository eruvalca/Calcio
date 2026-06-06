using Calcio.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calcio.Data.Configurations;

/// <summary>
/// Configures EF Core mapping for Calcio User Entity Configuration.
/// </summary>
public class CalcioUserEntityConfiguration : IEntityTypeConfiguration<CalcioUserEntity>
{
    /// <summary>
    /// Executes the Configure operation.
    /// </summary>
    /// <param name="builder">The builder.</param>
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
