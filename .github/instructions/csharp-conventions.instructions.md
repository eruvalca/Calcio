---
applyTo: "**/*.cs"
description: "Calcio C# coding conventions, editorconfig expectations, and logging rules."
---

# C# Conventions

- Follow the repository `.editorconfig`; it is the source of truth for formatting, naming, namespace, pattern matching, null-propagation, collection, and expression-style preferences.
- For braces and namespace style, follow `.editorconfig`; when it is not explicit, use braces for blocks and file-scoped namespaces.
- Prefer modern C# syntax unless the file's existing code demonstrably uses an older pattern for the same construct, in which case match that pattern for consistency.
- For empty-string style, follow `.editorconfig`; when it is not explicit, prefer `string.Empty`.
- Eliminate unused parameters and unused value assignments; warnings are enabled for these patterns.
- Prefer the C# 8+ declaration-form `using var x = …;` over the braced `using (var x = …) { }` form when the variable's lifetime naturally ends at the enclosing scope.

## Documentation

- Add XML documentation comments (`///`) for every C# type and member you add or modify, including `public`, `protected`, `internal`, and `private` declarations.
- Required coverage includes classes, records, structs, interfaces, enums, delegates, services, constructors, methods, properties, fields, and events.
- Every documented symbol must include a meaningful `<summary>` that explains purpose and behavior, not just a restatement of the symbol name.
- Add `<param>` for each method or constructor parameter. Add `<returns>` for non-`void` return values, including `Task<T>` and `ValueTask<T>`.
- Keep documentation behavior-accurate. When behavior changes, update docs in the same change.
- Generated files (for example `*.g.cs`, designer-generated files, and third-party generated sources) are excluded unless the generator supports doc customization.

## Logging

- Use source-generated logging via `partial` methods annotated with `LoggerMessage`.
- Inject `ILogger<T>` via the constructor in all classes that log. Do not use `ILoggerFactory` directly or create loggers outside of DI composition unless the class is a factory or host configuration component.
- Mark classes `partial` when they contain source-generated logging methods.
- If the target class is `static`, convert the logging methods to a separate non-static `partial` logging helper class, or document why source-generated logging cannot be applied and use a fallback `ILogger` passed as a parameter.
- Define one logging method per distinct message; keep messages short, stable, and template-based for structured sinks.
- Do not build log messages with interpolation or concatenation before logging. Pass structured values as method parameters.
- When logging exceptions, pass the `Exception` object as the first parameter of the `LoggerMessage` method and include only the structured context values (for example, operation name and resource identifier) needed to diagnose the failure. Do not swallow exceptions silently.
- Do not log PII or secrets. Include only identifiers necessary for diagnosis.
- Use `Trace` or `Debug` for internal state useful only during development, `Information` for significant application lifecycle events (for example, startup or configuration loaded), `Warning` for recoverable unexpected conditions, `Error` for failures that affect a single operation, and `Critical` for failures that require immediate intervention.
