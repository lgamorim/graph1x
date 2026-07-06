# Design Principles

- Always apply well-established object-oriented design practices: favor composition over inheritance, encapsulate invariants inside domain types, keep classes small and cohesive, and depend on abstractions at layer boundaries.
- Adhere to SOLID to keep the code adaptive to change:
    - **S** — one reason to change per class; split classes that mix concerns (e.g., validation + persistence).
    - **O** — extend behavior via new implementations of existing abstractions rather than modifying stable code with switch/if chains on type.
    - **L** — subtypes must honor base contracts; never throw `NotSupportedException` from an inherited member.
    - **I** — prefer small, role-specific interfaces (e.g., `IOrderReader` / `IOrderWriter`) over fat ones.
    - **D** — high-level modules (Application, Domain) define interfaces; Infrastructure implements them. Dependencies always point inward.
- Apply design patterns only when there is a tangible benefit compared to simpler alternatives. State the benefit in the PR/commit description when introducing one (e.g., Strategy to eliminate a growing conditional; Decorator for cross-cutting behavior). Do not introduce a pattern speculatively.
- IMPORTANT: Do not overengineer. No abstractions "for the future," no interfaces with a single implementation unless required for testing or layer isolation, no generic frameworks for one use case. Prefer the simplest design that satisfies current requirements and remains easy to change (YAGNI).
