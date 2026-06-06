---
applyTo: "Calcio/Calcio/Data/**/*.cs,Calcio/Calcio/Entities/**/*.cs,Calcio/Calcio/Services/**/*.cs,Calcio/Calcio.IntegrationTests/**/*.cs"
description: "Calcio EF Core, DbContext, entity, global query filter, and tenant isolation conventions."
---

# Data Access

## DbContext Usage

- `BaseDbContext` injects `IHttpContextAccessor`, resolves the current `CalcioUserEntity` id, and exposes `AccessibleClubIds` plus helper filters. Never new up a context without an accessor; tenancy enforcement depends on it.
- `ReadOnlyDbContext` sets `QueryTrackingBehavior.NoTracking`. `ReadOnlyDbContext.SaveChanges*` overloads throw `NotSupportedException` with the message `"Read-only context"`. Callers must not catch this exception; its presence indicates a programming error.
- `ReadWriteDbContext` inherits the same base behavior but enables saving. Register it for mutation handlers, EF migrations, and transactional work.
- Use `ReadOnlyDbContext` for all operations that do not write to the database, including single-record lookups, paginated lists, and bulk reads. Use `ReadWriteDbContext` only when `SaveChangesAsync` must be called.
- For request-scoped services, use the established repository pattern: inject scoped `IDbContextFactory<ReadOnlyDbContext>` and/or `IDbContextFactory<ReadWriteDbContext>`, create contexts with `CreateDbContextAsync(cancellationToken)`, and dispose them with `await using`.
- Ensure the current user context is established before creating a context, because global query filters capture the user id at context construction time. Do not inject DbContext instances directly into application services; use the factory pattern above. Direct DbContext injection is reserved for framework-owned infrastructure or tooling integration that requires it, and the call site must include a short explanatory comment.
- Never use `DbContext` directly in Blazor components or pages. All data access must go through service interfaces such as `IClubsService` and `IPlayersService`.

## Global Query Filters And Tenancy

- Club-scoped entities apply `IsOwnedByCurrentUser` filters so queries automatically restrict to `AccessibleClubIds`.
- Club-scoped entities include `ClubEntity`, `CampaignEntity`, `SeasonEntity`, `TeamEntity`, `PlayerEntity`, `NoteEntity`, `PlayerTagEntity`, `PlayerCampaignAssignmentEntity`, and `PlayerPhotoEntity`.
- `ClubJoinRequestEntity` is NOT in the `IsOwnedByCurrentUser` group. It uses a custom filter only: `r.RequestingUserId == currentUserId || accessibleClubIds.Contains(r.ClubId)`.
- There is no soft-delete global filter in this project. If a soft-delete pattern is introduced in future, it must be added to `BaseDbContext` alongside the tenancy filter and documented here.
- Build and test code must seed `IHttpContextAccessor.HttpContext` with the desired authenticated user before resolving services or creating DbContext instances; otherwise the filter captures the wrong user id for the context lifetime.
- When running outside a normal HTTP request, create a `DefaultHttpContext` with the intended `ClaimsPrincipal` and assign it to `IHttpContextAccessor.HttpContext` before the context is created. If a dedicated current-user abstraction is introduced later, update `BaseDbContext`, tests, and these instructions in the same change.
- `ClubEntity` filters on the membership join through `Club.CalcioUsers`. Queries should hide clubs unless the current user belongs to them.
- Avoid `IgnoreQueryFilters` unless the bypass is intentional and locally constrained. When returning club-scoped data to a caller, add an equivalent manual tenant check. Bypasses are acceptable for current-user identity checks, public club browsing, cache population scoped by current user, explicit tenancy-validation utilities, and integration tests that verify cascade, audit, or filter behavior. Add a short explanatory comment for non-obvious service-layer bypasses.

## Entity Patterns

- `ClubEntity` is the tenant root. Treat `ClubId` as the partition key for caching and data-sharding decisions.
- Removing a club cascades into seasons, campaigns, teams, players, notes, tags, join requests, assignments, and photos per entity configuration.
- `ClubJoinRequestEntity` enforces a unique constraint on `RequestingUserId` and cascades deletes from both the club and requesting user relationships.
- All entities inherit from `BaseEntity`, which provides `CreatedAt`, `CreatedById`, `ModifiedAt`, and `ModifiedById` audit fields.
- Use `required` for mandatory properties, such as `required long CreatedById`.
- Initialize nullable properties explicitly, such as `ModifiedAt { get; set; } = null`.
