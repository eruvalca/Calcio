---
applyTo: "Calcio.Shared/Endpoints/**/*.cs,Calcio/Endpoints/**/*.cs,Calcio.Client/Services/**/*.cs"
description: "Calcio API route constants, endpoint route templates, and client URL-builder conventions."
---

# API Routes

## Documentation

- All classes and members added or modified in route constants, endpoint registrations, or client route-builder usage must include XML documentation comments for all access levels, including private/internal members.
- Follow the canonical C# documentation rules in `.github/instructions/csharp-conventions.instructions.md` (Documentation section).

- All API route strings are centralized in `Calcio.Shared/Endpoints/Routes.cs`.
- Never use hardcoded route strings in endpoints or client services; always reference `Routes.*` constants.
- Do not rename or delete existing `Routes.*` constants without first searching for all usages across `Endpoints/**` and `Services/**` and updating them in the same change.
- `Routes.{Feature}.Group` provides route templates with parameter placeholders, such as `{clubId:long}`, for endpoint `MapGroup()` registration.
- `Routes.{Feature}.ForClub(clubId)` and similar methods build concrete URLs for `HttpClient` calls in client services.
- For routes with multiple parameters, name the builder method after all parameters in order, e.g., `ForClubAndPlayer(clubId, playerId)`, and include all parameters as method arguments in the same order they appear in the route template.
- `For*()` methods usually build path-only URLs. They may include query strings when the query value is part of a named, stable API operation; for example, `Routes.Clubs.ForBrowsing()` centralizes `?scope=all`.
- For ordinary optional filters, callers append query strings after calling the path builder. Keep query parameter names and well-known values as constants in `Routes.*` when they are part of the public API contract.
- When adding routes, always add a `Group` constant for endpoint registration. Add `For*()` URL-builder methods only when the route contains one or more parameters (e.g., `{clubId:long}`). Features whose routes have no parameters do not require `For*()` methods.
- Ensure each route template has exactly one matching URL-builder method signature pattern in the same feature block so endpoint registration and client URL generation stay aligned.
