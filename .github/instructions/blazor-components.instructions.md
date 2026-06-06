---
applyTo: "Calcio/Components/**/*.razor,Calcio/Components/**/*.razor.cs,Calcio.UI/Components/**/*.razor,Calcio.UI/Components/**/*.razor.cs,Calcio.Client/**/*.razor,Calcio.Client/**/*.razor.cs"
description: "Calcio Blazor component placement, render mode, code-behind, Bootstrap, and CSS conventions."
---

# Blazor Components

## Documentation

- All component code-behind classes and members added or modified must include XML documentation comments for all access levels, including private/internal members.
- Follow the canonical C# documentation rules in `.github/instructions/csharp-conventions.instructions.md` (Documentation section).

- Prefer static server-rendered pages and components by default.
- Only introduce interactive components, such as `@rendermode InteractiveAuto`, when interactivity is explicitly required.
- Never use `DbContext` directly in Blazor components or pages; use service interfaces.

## Page And Component Placement

- Use this placement decision procedure:
  1.  If the file has a `@page` directive, place it in the server project under `Calcio/Components/Pages/` for app-level pages or `Calcio/Components/{Feature}/Pages/` for feature pages, unless step 4 applies.
  2.  If the file is a non-interactive reusable component, place it in the server project under `Calcio/Components/{Feature}/Shared/`.
  3.  If the file is a reusable interactive component, such as a form or grid, place it in `Calcio.UI/Components/{Feature}/Shared/`.
  4.  Only place a page in `Calcio.UI` if every top-level section of the page requires an interactive render mode and extracting even one static wrapper component would provide no meaningful SSR benefit.
- App-level pages include root application pages such as `Home.razor`, `NotFound.razor`, and `Error.razor`; place them under `Calcio/Components/Pages/`.
- Feature pages include areas such as `Account`, `Clubs`, `Players`, and `Teams`; place them under `Calcio/Components/{Feature}/Pages/` when they are page roots.
- Layout files belong under `Calcio/Components/Layout/` or the existing layout folder for the feature. Do not create `Layout/Pages/` for routeable pages.
- Components shared across two or more features should live in the server project under `Calcio/Components/Shared/` when non-interactive or `Calcio.UI/Components/Shared/` when interactive.
- Pages in the server project should be static shells that provide layout, breadcrumbs, `PageTitle`, and host interactive components from `Calcio.UI`.
- This pattern preserves server-side rendering for page structure while deferring interactivity to specific components.

## Component Implementation

- All Blazor components inherit from `CancellableComponentBase` through `_Imports.razor` directives.
- All Blazor components must have a code-behind `.razor.cs` file except standard application files such as `App.razor`, `Routes.razor`, and `_Imports.razor`, or components whose entire markup and logic fit within ~20 lines and contain no injected dependencies.
- Always use primary constructors in code-behind files to inject dependencies.

## Bootstrap And CSS

- The front end uses Bootstrap 5.3.
- Prefer Bootstrap grid and component styles for consistency.
- Ensure UI remains responsive across viewport sizes.
- Avoid custom components or custom styling when stock Bootstrap patterns are sufficient.
- Prefer `rem` over `px` for spacing, sizing, and typography.
- Keep `px` for borders, small box-shadow values, and outlines where precise rendering matters.
