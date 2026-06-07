---
applyTo: "Calcio.Unit.Tests/**/*.cs,Calcio.Unit.Tests/**/*.razor,Calcio.Integration.Tests/**/*.cs"
description: "Calcio unit and integration test conventions, fixtures, libraries, and ServiceResult assertions."
---

# Testing

## Documentation

- Test-support classes and helper members added or modified must include XML documentation comments for all access levels, including private/internal members.
- Follow the canonical C# documentation rules in `.github/instructions/csharp-conventions.instructions.md` (Documentation section).

- When writing code for new features or fixing bugs, include appropriate unit or integration tests for the changed behavior.
- If the behavior cannot be unit tested due to missing abstractions or static dependencies, add an integration test instead and leave a TODO comment referencing the testability gap.
- Run and verify all tests in the project(s) directly affected by the change (e.g., Calcio.Unit.Tests and/or Calcio.Integration.Tests) pass before considering the task complete.

## Unit Tests

- Name test methods using the pattern `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `GetUser_NonExistentId_ReturnsProblem`).

- Unit tests live in `Calcio.Unit.Tests/`.
- Use bUnit for Blazor component tests with `BunitContext` as the base class.
- Use NSubstitute for mocking dependencies.
- Use Shouldly for assertions.
- Use `RichardSzalay.MockHttp` for mocking `HttpClient` in client service tests.
- For success cases, assert both `.IsSuccess.ShouldBeTrue()` and `.Value` contents. For failure cases, assert both `.IsProblem.ShouldBeTrue()` and the specific `.Problem.Kind` enum value.

## Integration Tests

- Integration tests live in `Calcio.Integration.Tests/`.
- Use `CustomApplicationFactory` for test fixture setup.
- Inherit from `BaseDbContextTests` for database-dependent tests.
- Create test entities by calling methods on the DbContext obtained from `scope.ServiceProvider` directly inside each test or a shared setup method; do not rely on pre-seeded data from migrations.
- Use `SetCurrentUser(scope.ServiceProvider, userId)` to set test user context.
- Test global query filter behavior: when an authenticated user queries records owned by a different user/tenant (i.e., filtered out by the EF Core global query filter), the result set is empty rather than returning a ServiceProblem.
- Verify `ServiceProblemKind` values for error scenarios.
