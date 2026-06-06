using Calcio.Entities.Base;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Calcio.Data.Interceptors;

/// <summary>
/// Represents the Audit Save Changes Interceptor.
/// </summary>
/// <param name="httpContextAccessor">The http Context Accessor.</param>
/// <param name="timeProvider">The time Provider.</param>
public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <summary>
    /// Executes the Try Get Current User Id operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private long? TryGetCurrentUserId()
    {
        var primitive = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(primitive) ? null : long.Parse(primitive);
    }

    /// <summary>
    /// Executes the Saving Changes operation.
    /// </summary>
    /// <param name="eventData">The event Data.</param>
    /// <param name="result">The result.</param>
    /// <returns>The operation result.</returns>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Executes the Saving Changes Async operation.
    /// </summary>
    /// <param name="eventData">The event Data.</param>
    /// <param name="result">The result.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Executes the Stamp Audit Fields operation.
    /// </summary>
    /// <param name="context">The context.</param>
    private void StampAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();
        if (entries.Count == 0)
        {
            return;
        }

        var userId = TryGetCurrentUserId() ?? throw new InvalidOperationException("Current CalcioUserId is null when trying to create or update an entity.");
        var now = timeProvider.GetUtcNow();
        var createdTimestamp = now;
        var modifiedTimestamp = now;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // CreatedById is a required property set by the service layer.
                // We only set timestamps here; CreatedById is trusted from the entity.
                entry.Entity.CreatedAt = createdTimestamp;
                entry.Entity.ModifiedAt = modifiedTimestamp;
                entry.Entity.ModifiedById = userId;
            }
            else // Modified
            {
                entry.Entity.ModifiedAt = modifiedTimestamp;
                entry.Entity.ModifiedById = userId;
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedById).IsModified = false;
            }
        }
    }
}
