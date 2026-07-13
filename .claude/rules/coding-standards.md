# Coding Standards

Always follow the Microsoft C# coding conventions and .NET naming guidelines:

- PascalCase for types, methods, properties, constants, and public members; camelCase for locals and parameters; `_camelCase` for private fields (private static readonly values are constant-like and stay PascalCase); `I` prefix for interfaces; `Async` suffix for async methods.
- One top-level type per file; file name matches the type name.
- Use file-scoped namespaces, `var` when the type is apparent, expression-bodied members only when they improve readability.
- Enable and respect nullable reference types (`<Nullable>enable</Nullable>`); never suppress warnings with `!` without a comment justifying it.
- Prefer records for immutable data, `readonly` where possible, and pattern matching over type checks/casts.
- Public APIs must have XML doc comments; internal code is documented only where intent is not obvious from the code. Test projects are exempt — their `public` types exist only for test-framework discovery, not as a consumed API surface.
- Never include unnecessary using directives.
- Codify these conventions in an `.editorconfig` at the solution root (naming rules, `file_scoped` namespaces, `IDE0005` for unused usings, etc.) so they are tool-enforced rather than prose-only, and set `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` so style violations fail the build alongside `TreatWarningsAsErrors`.
- All code must pass `dotnet format` and build with zero warnings (`TreatWarningsAsErrors` is on).
