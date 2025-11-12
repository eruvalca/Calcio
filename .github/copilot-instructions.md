# Repository overview

- This solution is a .NET 10 Blazor web app.
- The server/host project for the application is `D:\repos\Calcio\Calcio\Calcio\Calcio.csproj`.
- The Blazor WebAssembly project for interactive components is `D:\repos\Calcio\Calcio.Client\Calcio.Client.csproj`.
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