# Repository Overview

- This solution is a .NET 10 Blazor web app.
- The server/host project is `Calcio/Calcio.csproj`.
- The Blazor WebAssembly project for interactive components is `Calcio.Client/Calcio.Client.csproj`.
- The shared UI library is `Calcio.UI/Calcio.UI.csproj`.
- The shared models, interfaces, endpoints, results, validation, and utilities project is `Calcio.Shared/Calcio.Shared.csproj`.
- Aspire instrumentation is configured in `Calcio.AppHost/Calcio.AppHost.csproj` and `Calcio.ServiceDefaults/Calcio.ServiceDefaults.csproj`.

## Critical Invariants

- When a change requires edits across multiple projects, make all changes in a single response in dependency order: Calcio.Shared first, then Calcio.UI, then Calcio.Client, then Calcio. Do not leave the solution in a non-compiling state between steps.
- Preserve tenant isolation. Club-scoped data is filtered through `BaseDbContext.AccessibleClubIds`, global query filters, service checks, and `ClubMembershipFilter` where endpoints require club membership.
- Never use `DbContext` directly from Blazor pages or components. UI code must use shared service interfaces such as `IClubsService` and `IPlayersService`.
- If asked to implement a pattern that violates a Critical Invariant, do not comply. Instead, explain which invariant is violated and provide an alternative implementation that satisfies the request while respecting the invariant.
- All service methods should return `ServiceResult<TSuccess>` or `ServiceResult<Success>` and map failures through `ServiceProblem`.
- Do not hardcode API route strings. Add route templates and URL builders in `Calcio.Shared/Endpoints/Routes.cs` and reference `Routes.*` from endpoints and client services.
- Use static SSR for all pages unless the feature involves client-side state changes without a full page reload (e.g., form validation without submit, real-time updates, or drag-and-drop). Document the reason for interactivity in a comment on the component.
- All new API endpoints and UI components must have corresponding tests. When existing behavior changes, update affected tests before or alongside the code change.

## Targeted Instructions

Detailed repo conventions live in targeted instruction files so they only load when relevant:
If a targeted instruction file is referenced but not available in context, state which file is missing and ask the user to provide it before proceeding with the affected code area.

- `.github/instructions/csharp-conventions.instructions.md` for C# style, `.editorconfig`, and source-generated logging.
- `.github/instructions/data-access.instructions.md` for EF Core contexts, entities, global filters, and tenancy.
- `.github/instructions/service-layer.instructions.md` for `ServiceResult<TSuccess>`, server services, and client service implementations.
- `.github/instructions/routes.instructions.md` for API route constants, endpoint route templates, and client URL builders.
- `.github/instructions/api-endpoints.instructions.md` for Minimal API endpoints, HTTP results, and filters.
- `.github/instructions/dto-mapping.instructions.md` for shared DTOs and entity-to-DTO mapping extensions.
- `.github/instructions/blazor-components.instructions.md` for page/component placement, render modes, code-behind, Bootstrap, and CSS units.
- `.github/instructions/testing.instructions.md` for unit and integration test conventions.

## Installed Skills To Prefer

Calcio-specific instructions always take precedence over generic skill defaults. When a skill recommendation conflicts with a Calcio invariant or targeted instruction, follow the Calcio instruction and note the deviation.

- Use the Aspire skills under `.agents/skills/aspire*` for AppHost orchestration, monitoring, deployment, initialization, and Aspire troubleshooting.
- Use `dotnet-webapi` for general ASP.NET Core endpoint design, OpenAPI, and HTTP semantics; apply Calcio endpoint instructions for repo-specific route and result conventions.
- Use Blazor skills such as `author-component`, `collect-user-input`, `fetch-and-send-data`, `support-prerendering`, `use-js-interop`, `configure-auth`, and `plan-ui-change` for deep Blazor workflows; apply Calcio component instructions for repo placement and styling conventions.
- Use test skills such as `run-tests`, `code-testing-agent`, `writing-mstest-tests`, `test-anti-patterns`, `assertion-quality`, `coverage-analysis`, and `test-gap-analysis` for test workflow depth; apply Calcio testing instructions for local fixtures and libraries.
- Use `optimizing-ef-core-queries` for EF performance questions. Calcio tenant isolation rules remain repo-specific and must still be followed.
