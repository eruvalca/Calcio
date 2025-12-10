# Repository Overview

- This solution is a .NET 10 Blazor web app.
- The server/host project for the application is `D:\repos\Calcio\Calcio\Calcio\Calcio.csproj`.
- The Blazor WebAssembly project for interactive components is `D:\repos\Calcio\Calcio\Calcio.Client\Calcio.Client.csproj`.
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
- Always use `string.Empty` instead of `""`.

### Naming

- Interfaces: PascalCase starting with `I`.
- Types & non-field members: PascalCase.

### Performance & Quality

- Eliminate unused parameters (warnings enabled) and unused value assignments (discard where needed).

### Using & Resource Management

- Prefer simple `using` statements (no extra block) where possible.

### Logging

- Always use source-generated logging via `partial` methods annotated with `LoggerMessage`. Classes using source-generated logging must also be marked `partial`.
- Define one method per distinct log message; keep messages short, stable, and template-based for structured sinks.
- Avoid runtime string building (interpolation, concatenation) before logging; pass state via method parameters.
- Prefer strongly-typed logging methods inside loops and hot code paths for minimal allocations.
- Do not log PII or secrets. Include only identifiers necessary for diagnosis.
- Use appropriate log level (Trace/Debug/Information/Warning/Error/Critical) and avoid elevating routine events.

## Data Access Layer Guidance

### DbContext Usage

- `BaseDbContext` injects `IHttpContextAccessor`, resolves the current `CalcioUserEntity` id, and exposes `AccessibleClubIds` plus helper filters. Never new up a context without an accessor; tenancy enforcement depends on it.
- `ReadOnlyDbContext` sets `QueryTrackingBehavior.NoTracking` and throws on `SaveChanges*`. Use it for query pipelines, read models, and background projections where writes are disallowed.
- `ReadWriteDbContext` inherits the same base behavior but enables saving. Register it for mutation handlers, EF migrations, and transactional work. Keep read-heavy operations in `ReadOnlyDbContext` to avoid accidental tracking.
- Use `IDbContextFactory<T>` with `CreateDbContextAsync` instead of injecting `DbContext` directly; ensures proper scoping and allows `await using` disposal.
- **Never use DbContext directly in Blazor components or pages.** All data access must go through service interfaces (e.g., `IClubsService`, `IPlayersService`). This ensures separation of concerns, testability, and consistent behavior between server and client rendering modes.

### Global Query Filters & Tenancy

- All club-scoped entities (`ClubEntity`, `CampaignEntity`, `SeasonEntity`, `TeamEntity`, `PlayerEntity`, `NoteEntity`, `PlayerTagEntity`, `PlayerCampaignAssignmentEntity`, `PlayerPhotoEntity`) apply `IsOwnedByCurrentUser` filters so queries automatically restrict to `AccessibleClubIds`.
- `ClubJoinRequestEntity` uses a custom filter: the requesting user always sees their own requests, and club members see requests targeting their clubs via `AccessibleClubIds`. Build/testing code must resolve scopes with the desired user _before_ resolving `ReadWriteDbContext`, otherwise the filter captures the wrong user id for the context lifetime.
- `ClubEntity` itself filters on the membership join (`Club.CalcioUsers`). Expect queries to automatically hide clubs unless the current user belongs to them.
- Avoid bypassing filters (e.g., `IgnoreQueryFilters`) unless you are in infrastructure code validating tenancy constraints; any bypass must include manual club checks.

### Entity Relationships & Expectations

- `ClubEntity` (`ClubId` key) acts as the tenant root; every user, campaign, season, team, player, note, tag, join request, assignment, and photo hangs off a club id. Treat `ClubId` as the partition key for caching and data sharding decisions.
- Removing a club cascades into seasons, campaigns, teams, players, notes, tags, join requests, and related assignments/photos per entity configuration.
- `ClubJoinRequestEntity` enforces a unique constraint on `RequestingUserId` (one open request per user) and cascades deletes from both the club and requesting user relationships.

### Entity Patterns

- All entities inherit from `BaseEntity` which provides `CreatedAt`, `CreatedById`, `ModifiedAt`, and `ModifiedById` audit fields.
- Use `required` modifier for mandatory properties (e.g., `required long CreatedById`).
- Initialize nullable properties explicitly (e.g., `ModifiedAt { get; set; } = null`).

---

## Service Layer Patterns

### ServiceResult & ServiceProblem

- Use `ServiceResult<TSuccess>` (from `Calcio.Shared.Results`) as the return type for all service methods.
- `ServiceResult<TSuccess>` is a discriminated union: either a success value or a `ServiceProblem`.
- Use `ServiceProblem` static factory methods: `NotFound()`, `Forbidden()`, `Conflict()`, `BadRequest()`, `ServerError()`.
- Check results with `.IsSuccess` / `.IsProblem` properties; access `.Value` or `.Problem` accordingly.
- Use `.Match<TResult>()` in endpoints to convert to HTTP results; use `.Switch()` in components for side effects.
- For void-like operations, use `ServiceResult<Success>` with `OneOf.Types.Success`.
- 401 Unauthorized is handled at the middleware layer, not in service results.

### Server-side Services

- Services requiring authenticated user context should inherit from `AuthenticatedServiceBase` to access `CurrentUserId`.
- Make service classes `partial` when using source-generated logging.
- Return `ServiceProblem.NotFound()` when entities are not found or hidden by global query filters.
- Return `ServiceProblem.Conflict()` for business rule violations (e.g., duplicate requests).

### Client-side Services (Blazor WASM)

- Implement shared service interfaces (from `Calcio.Shared`) using `HttpClient`.
- Map HTTP status codes to `ServiceProblem` using switch expressions.
- Register services via `AddHttpClient<TInterface, TImplementation>()` with base address.
- Return `ServiceProblem.NotFound()` for 404, `ServiceProblem.Forbidden()` for 403, `ServiceProblem.Conflict()` for 409, `ServiceProblem.ServerError()` as fallback.

## Minimal API Endpoints

- Place endpoints in `Endpoints/` folder, organized by feature.
- Use `MapGroup()` with `.RequireAuthorization()`.
- Always use `TypedResults` (not `Results`) and declare explicit `Results<T1, T2, ...>` return types.
- Use `ProblemHttpResult` as the single error result type; all HTTP errors return RFC 7807 ProblemDetails.
- Use `.ProducesProblem(StatusCodes.StatusXXX)` on route groups to document common error responses (401, 403, 500).
- Use `.ProducesProblem()` on individual endpoints for endpoint-specific errors (404, 409).
- Return errors via `TypedResults.Problem(statusCode: StatusCodes.StatusXXX)` for consistent ProblemDetails format.
- Convert `ServiceResult<T>` to HTTP results using `.ToHttpResult()` extension methods (from `Calcio.Endpoints.Extensions`).
- Use `ClubMembershipFilter` for endpoints requiring club membership validation.
- API routes (`/api/*`) use ProblemDetails via `UseExceptionHandler()` and `UseStatusCodePages()`.
- Non-API routes use `UseStatusCodePagesWithReExecute("/not-found")` for user-friendly error pages.

## Endpoint Filters

- `ClubMembershipFilter`: validates the user belongs to the club specified by `clubId` route parameter before the endpoint executes. Returns 403 Forbidden if unauthorized.

## DTOs & Extensions

- Place DTOs in `Calcio.Shared/DTOs/{Feature}/` as `record` types with positional parameters.
- Use C# 14 extension members syntax for entity-to-DTO mappings in `Calcio.Shared/Extensions/{Feature}/`.
- Naming convention: `{Entity}Extensions.cs` containing `To{Dto}()` methods.

## Blazor Components

### Page vs Component Placement

- **Pages** (components with `@page` directive) should live in the server project (`Calcio/Components/Pages/`) organized by feature.
- **Interactive components** (forms, grids, etc. that require `@rendermode InteractiveAuto`) should live in the UI project (`Calcio.UI/Components/{Feature}/Shared/`).
- Pages in the server project should be static shells that provide layout, breadcrumbs, `PageTitle`, and host interactive components from `Calcio.UI`.
- Only place a page in `Calcio.UI` if the entire page root requires interactivity and cannot be split.
- This pattern enables server-side rendering for page structure while deferring interactivity to specific components.

### Component Guidelines

- All blazor components inherit from `CancellableComponentBase` to support cancellation tokens. This is set with `@inherits` directives in `_Imports.razor` files.
- Most blazor components should have a code-behind `.razor.cs` file for C# code. With the exception of standard blazor application files (e.g. `App.razor`, `Routes.razor`, `_Imports.razor`), all `.razor` files should have a corresponding `.razor.cs` file.
- Always use primary constructors in code-behind files to inject dependencies.

### Bootstrap

- The front-end for this application uses Bootstrap 5.3.
- Ensure that any new UI components or pages adhere to Bootstrap's grid system and component styles for consistency.
- Ensure responsiveness across different device sizes.
- Stock bootstrap components and styles should be used wherever possible to maintain a consistent look and feel and custom implementations or styling should be avoided unless absolutely necessary.

## Testing

### Unit Tests (Calcio.UnitTests)

- Use bUnit for Blazor component tests with `BunitContext` base class.
- Use NSubstitute for mocking dependencies.
- Use Shouldly for assertions.
- Use `RichardSzalay.MockHttp` for mocking HttpClient in client service tests.
- Test `ServiceResult` outcomes with `.IsSuccess`, `.IsProblem`, `.Value`, and `.Problem.Kind`.

### Integration Tests (Calcio.IntegrationTests)

- Use `CustomApplicationFactory` for test fixture setup.
- Inherit from `BaseDbContextTests` for database-dependent tests.
- Use `SetCurrentUser(scope.ServiceProvider, userId)` to set test user context.
- Test global query filter behavior: unauthorized access returns empty results, not errors.
- Verify `ServiceProblemKind` values for error scenarios.

### Testing Guidelines

- When writing code for new features or fixing bugs, always include appropriate unit and/or integration tests to cover the changes.
- When modifying existing code, ensure that existing tests are updated as necessary to reflect the changes and that all tests pass successfully before finalizing the changes.
