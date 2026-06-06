---
applyTo: "Calcio/Calcio/Endpoints/**/*.cs"
description: "Use when creating or modifying Calcio Minimal API endpoints, endpoint filters, HTTP results, ProblemDetails responses, or ServiceResult-to-HTTP mapping."
---

# API Endpoints

Route constants and URL builders are covered by `.github/instructions/routes.instructions.md`.
DTO and entity mapping conventions are covered by `.github/instructions/dto-mapping.instructions.md`.

## Minimal API Patterns

- Place endpoints in `Endpoints/`, organized by feature.
- Use `MapGroup(Routes.{Feature}.Group).RequireAuthorization()` for feature groups. Individual endpoints that must be publicly accessible must explicitly call `.AllowAnonymous()` on the endpoint registration. If a feature is entirely public, create a separate unauthenticated group without `.RequireAuthorization()`.
- Use `TypedResults`, not `Results`.
- Declare explicit `Results<T1, T2, ...>` return types.
- Keep HTTP error handling in two layers:

  | Layer                 | Rule                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
  | --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
  | OpenAPI metadata only | Add `.ProducesProblem(StatusCodes.Status401Unauthorized)`, `.ProducesProblem(StatusCodes.Status403Forbidden)`, and `.ProducesProblem(StatusCodes.Status500InternalServerError)` to every route group. Add `.ProducesProblem(StatusCodes.Status404NotFound)`, `.ProducesProblem(StatusCodes.Status409Conflict)`, or other endpoint-specific errors to individual endpoints that can return those codes. These declarations are metadata only and do not produce runtime responses.              |
  | Runtime results       | Declare `ProblemHttpResult` as the single error result type in `Results<T1, T2, ...>` unions. Use `.ToHttpResult()` from `Calcio.Endpoints.Extensions` (`Calcio/Calcio/Endpoints/Extensions/ServiceResultExtensions.cs`) exclusively when returning the result of a service call; it maps `ServiceResult<T>` failures to `TypedResults.Problem(...)` automatically, so do not add a separate `TypedResults.Problem(...)` after `.ToHttpResult()`. Use `TypedResults.Problem(statusCode: StatusCodes.StatusXXX)` directly only for errors detected in the endpoint handler before calling a service. |

- API routes under `/api/*` use ProblemDetails via `UseExceptionHandler()` and `UseStatusCodePages()`.
- Non-API routes use `UseStatusCodePagesWithReExecute("/not-found")` for user-friendly error pages.

## Endpoint Filters

- Use `ClubMembershipFilter` for endpoints requiring club membership validation.
- Only apply `ClubMembershipFilter` to endpoints whose effective route template includes a `clubId` segment. The effective template is the `MapGroup(...)` route combined with the `MapGet`, `MapPost`, or other `Map*` sub-route.
- Constrained route parameters such as `{clubId:long}` satisfy the `clubId` requirement. Prefer route constants such as `Routes.Parameters.ClubIdLong` when adding new group or endpoint templates.
- When generating an endpoint that uses `ClubMembershipFilter`, verify that the effective route template contains `{clubId`. If it does not, do not add `ClubMembershipFilter`; update the route to include a `clubId` parameter before applying the filter.
- `ClubMembershipFilter` validates that the current user belongs to the club specified by the `clubId` route parameter and returns 403 Forbidden when unauthorized.
