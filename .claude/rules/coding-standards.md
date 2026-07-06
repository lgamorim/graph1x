# Coding Standards

Always follow the Microsoft C# coding conventions and .NET naming guidelines:

- PascalCase for types, methods, properties, constants, and public members; camelCase for locals and parameters; `_camelCase` for private fields; `I` prefix for interfaces; `Async` suffix for async methods.
- One top-level type per file; file name matches the type name.
- Use file-scoped namespaces, `var` when the type is apparent, expression-bodied members only when they improve readability.
- Enable and respect nullable reference types (`<Nullable>enable</Nullable>`); never suppress warnings with `!` without a comment justifying it.
- Prefer records for immutable data, `readonly` where possible, and pattern matching over type checks/casts.
- Public APIs must have XML doc comments; internal code is documented only where intent is not obvious from the code.
- Never include unnecessary using directives.
- All code must pass `dotnet format` and build with zero warnings (`TreatWarningsAsErrors` is on).
