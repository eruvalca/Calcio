---
applyTo: "Calcio/Calcio.Shared/DTOs/**/*.cs,Calcio/Calcio/Extensions/**/*.cs"
description: "Calcio shared DTO and entity-to-DTO mapping extension conventions."
---

# DTOs And Mapping Extensions

## Documentation

- All DTO and mapping-extension classes/members added or modified must include XML documentation comments for all access levels, including private/internal members.
- Follow the canonical C# documentation rules in `.github/instructions/csharp-conventions.instructions.md` (Documentation section).

- Place DTOs in `Calcio.Shared/DTOs/{Feature}/` as record types with positional parameters.
- Place entity-to-DTO mapping extensions in `Calcio/Extensions/{Feature}/`.
- Use C# 14 extension members syntax for entity-to-DTO mappings; target the .NET 10 SDK with `<LangVersion>preview</LangVersion>`, for example: `extension(Player entity) { public PlayerDto ToPlayerDto() => new(...); }`.
- If a DTO requires data from more than one entity or computed values, add the required parameters to the `To{Dto}()` method signature rather than creating a separate helper class.
- Name mapping files `{Entity}Extensions.cs` and mapping methods `To{Dto}()`. Do not generate DTO-to-entity mapping methods unless explicitly requested. If needed, place them in the same `{Entity}Extensions.cs` file and name them `To{Entity}()`.
