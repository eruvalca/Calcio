using Calcio.Shared.Entities.Base;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Calcio.Data.Interceptors;

public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private long? TryGetCurrentUserId()
    {
        var primitive = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(primitive) ? null : long.Parse(primitive);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

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
