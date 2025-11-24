using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts;

public class ReadOnlyDbContext : ReadWriteDbContext
{
    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options, httpContextAccessor)
        => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    public override int SaveChanges() => throw new NotSupportedException("Read-only context");
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new NotSupportedException("Read-only context");
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Read-only context");
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Read-only context");
}
