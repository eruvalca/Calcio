---
applyTo: "Calcio/Services/**/*.cs,Calcio.Client/Services/**/*.cs,Calcio.Shared/Services/**/*.cs,Calcio.Shared/Results/**/*.cs"
description: "Calcio service layer conventions for ServiceResult, ServiceProblem, server services, client services, and result primitives."
---

# Service Layer

These conventions also apply when editing `Calcio.Shared.Results` primitives and HTTP response mapping helpers.

## Documentation

- All classes, interfaces, and methods added or modified in service-layer files must include XML documentation comments for all access levels, including private/internal members.
- Follow the canonical C# documentation rules in `.github/instructions/csharp-conventions.instructions.md` (Documentation section).

## ServiceResult And ServiceProblem

- Use `ServiceResult<TSuccess>` from `Calcio.Shared.Results` as the return type for service methods.
- Use `ServiceResult<Success>` with `OneOf.Types.Success` for void-like operations.
- `ServiceResult<TSuccess>` is a discriminated union: either a success value or a `ServiceProblem`.
- Use `ServiceProblem` static factory methods: `NotFound()`, `Forbidden()`, `Conflict()`, `BadRequest()`, and `ServerError()`.
- Check results with `.IsSuccess` and `.IsProblem`; access `.Value` or `.Problem` accordingly.
- Use `.Match<TResult>()` in endpoints to convert to HTTP results and `.Switch()` in components for side effects.
- 401 Unauthorized is handled at middleware, not in service results.

## Shared Service Contracts

- Files under `Calcio.Shared/Services/**/*.cs` define shared service interfaces and cross-project contracts.
- Keep shared service contracts free of server-only base classes, EF Core types, and implementation details.
- Authentication-specific implementation guidance, such as inheriting from `AuthenticatedServiceBase`, applies to server services under `Calcio/Services/**/*.cs`, not to shared interfaces.
- Client implementations under `Calcio.Client/Services/**/*.cs` implement the shared interfaces using `HttpClient`.

## Server-side Services

- Services requiring authenticated user context should inherit from `AuthenticatedServiceBase` to access `CurrentUserId`.
- Return `ServiceProblem.NotFound()` when an entity does not exist in the database OR when it is excluded from results by a global query filter (e.g., soft-delete or tenant filter). Do not distinguish between these two cases; treat both as NotFound from the caller's perspective.
- Return `ServiceProblem.Conflict()` specifically for concurrency or uniqueness violations (e.g., duplicate submission, optimistic concurrency failure). Return `ServiceProblem.BadRequest()` for other business rule violations where the client input is structurally valid but semantically rejected (e.g., invalid state transition).
- Accept a `CancellationToken` parameter in all async service methods and forward it to all async database and downstream calls.
- Return `ServiceProblem` for expected domain, validation, authorization, conflict, not-found, and recoverable downstream failures. Unexpected programming, EF, or infrastructure exceptions may propagate to ASP.NET Core exception handling unless the service has a specific recovery or mapping reason.
- Catch and map exceptions only when the service owns the failure semantics, such as parser errors that should become `ServiceProblem.BadRequest()` or external dependency failures that should become `ServiceProblem.ServerError()`. Log mapped unexpected failures with structured context before returning.
- Make service classes `partial` when using source-generated logging.

## Client-side Services

- Implement shared service interfaces from `Calcio.Shared` using `HttpClient`.
- Register services via `AddHttpClient<TInterface, TImplementation>()` with base address.
- Use `HttpResponseMessage.ToServiceProblemAsync()` from `Calcio.Shared.Results` to convert error responses to `ServiceProblem`.
- `ToServiceProblemAsync()` maps HTTP status codes to `ServiceProblemKind`: 404 to `NotFound`, 403 to `Forbidden`, 409 to `Conflict`, 400 to `BadRequest`, and other errors to `ServerError`.
- Use the existing client-service pattern: `return response.IsSuccessStatusCode ? successResult : await response.ToServiceProblemAsync(cancellationToken);`.
- If deserialization of a successful response fails, return `ServiceProblem.ServerError()` rather than throwing. Wrap the deserialization call in a try/catch and map `JsonException` or a null result to `ServiceProblem.ServerError()`.
