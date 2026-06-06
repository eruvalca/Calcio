using Microsoft.EntityFrameworkCore;

namespace Calcio.Data.Contexts;

/// <summary>
/// Represents the Read Only Db Context.
/// </summary>
public class ReadOnlyDbContext : ReadWriteDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options, httpContextAccessor)
        => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    /// <summary>
    /// Executes the Save Changes operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public override int SaveChanges() => throw new NotSupportedException("Read-only context");
    /// <summary>
    /// Executes the Save Changes operation.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">The accept All Changes On Success.</param>
    /// <returns>The operation result.</returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new NotSupportedException("Read-only context");
    /// <summary>
    /// Executes the Save Changes Async operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Read-only context");
    /// <summary>
    /// Executes the Save Changes Async operation.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">The accept All Changes On Success.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Read-only context");
}
