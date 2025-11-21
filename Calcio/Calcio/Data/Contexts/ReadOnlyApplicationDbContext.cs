using Calcio.Data.Contexts.Base;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts;

public class ReadOnlyApplicationDbContext : BaseDbContext
{
    public ReadOnlyApplicationDbContext(DbContextOptions<BaseDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options, httpContextAccessor) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    public override int SaveChanges() => throw new NotSupportedException("Read-only context");
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("Read-only context");
}
