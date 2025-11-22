# Repository overview

- This solution is a .NET 10 Blazor web app.
- The server/host project for the application is `D:\repos\Calcio\Calcio\Calcio\Calcio.csproj`.
- The Blazor WebAssembly project for interactive components is `D:\repos\Calcio\Calcio.Client\Calcio.Client.csproj`.
- The shared UI library containing reusable components and layouts is `D:\repos\Calcio\Calcio\Calcio.UI\Calcio.UI.csproj`.
- The `D:\repos\Calcio\Calcio\Calcio.Shared\Calcio.Shared.csproj` project contains shared models, interfaces, utilities, etc. used by both server and client projects.
- Aspire instrumentation is configured in `D:\repos\Calcio\Calcio.AppHost\Calcio.AppHost.csproj` and `D:\repos\Calcio\Calcio.ServiceDefaults\Calcio.ServiceDefaults.csproj`.

## C# / .NET Coding Guidelines

### Formatting & Structure

- Always use braces for blocks.
- Use file-scoped namespaces exclusively.

### Language Features & Expressions

- Prefer pattern matching for type + null checks and logical simplification.
- Use switch expressions where suitable.
- Use null-propagation (`?.`) and coalescing (`??`) operators when appropriate.
- Prefer conditional expressions over separate assignment/return branches when clear.
- Use compound assignments (`+=`, `??=` etc.) when they improve clarity.
- Prefer range (`x[a..b]`) and index (`x[^1]`) operators where expressive.
- Allow throw expressions in ternaries / null-coalescing (`csharp_style_throw_expression = true`).
- Use implicit object creation (`new()`) when type apparent.

### Null / Equality / Safety

- Prefer `is null` pattern over `ReferenceEquals`.
- Avoid redundant null checks when null-propagation suffices.

### Members & Types

- Prefer auto-properties over fields when suitable.
- Mark fields `readonly` when possible.
- Use explicit tuple & anonymous member names inference.

### Naming

- Interfaces: PascalCase starting with `I`.
- Types & non-field members: PascalCase.

### Performance & Quality

- Eliminate unused parameters (warnings enabled) and unused value assignments (discard where needed).

### Using & Resource Management

- Prefer simple `using` statements (no extra block) where possible.

### Blazor Components

- All blazor components inherit from `CancellableComponentBase` to support cancellation tokens. This is set with `@inherits` directives in `_Imports.razor` files.
- Most blazor components should have a code-behind `.razor.cs` file for C# code. With the exception of standard blazor application files (e.g. `App.razor`, `Routes.razor`, `_Imports.razor`), all `.razor` files should have a corresponding `.razor.cs` file.

## Data Access Layer Guidance

### DbContext usage

- `BaseDbContext` injects `IHttpContextAccessor`, resolves the current `CalcioUserEntity` id, and exposes `AccessibleClubIds` plus helper filters. Never new up a context without an accessor; tenancy enforcement depends on it.
- `ReadOnlyDbContext` sets `QueryTrackingBehavior.NoTracking` and throws on `SaveChanges*`. Use it for query pipelines, read models, and background projections where writes are disallowed.
- `ReadWriteDbContext` inherits the same base behavior but enables saving. Register it for mutation handlers, EF migrations, and transactional work. Keep read-heavy operations in `ReadOnlyDbContext` to avoid accidental tracking.

### Global query filters & tenancy

- All club-scoped entities (`ClubEntity`, `CampaignEntity`, `SeasonEntity`, `TeamEntity`, `PlayerEntity`, `NoteEntity`, `PlayerTagEntity`, `PlayerCampaignAssignmentEntity`, `PlayerPhotoEntity`) apply `IsOwnedByCurrentUser` filters so queries automatically restrict to `AccessibleClubIds`.
- `ClubEntity` itself filters on the membership join (`Club.CalcioUsers`). Expect queries to automatically hide clubs unless the current user belongs to them.
- Avoid bypassing filters (e.g., `IgnoreQueryFilters`) unless you are in infrastructure code validating tenancy constraints; any bypass must include manual club checks.

### Entity relationships & expectations

- `ClubEntity` (`ClubId` key) acts as the tenant root; every user, campaign, season, team, player, note, tag, join request, assignment, and photo hangs off a club id. Treat `ClubId` as the partition key for caching and data sharding decisions.
- Removing a club cascades into seasons, campaigns, teams, players, notes, tags, join requests, and related assignments/photos per entity configuration.
