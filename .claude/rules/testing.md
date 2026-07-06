# Testing — Test-Driven Development

YOU MUST follow a test-driven development approach for all production code:

1. **Red** — write a failing test that specifies the desired behavior before writing implementation code.
2. **Green** — write the minimum implementation to make the test pass.
3. **Refactor** — clean up the code and tests while keeping everything green.

Rules:

- Never write production code without a failing test that motivates it. Never mark a task complete while any test fails.
- Ensure all relevant edge cases are covered: null/empty inputs, boundary values, invalid state transitions, concurrency where applicable, and failure paths (exceptions, timeouts, cancellation via `CancellationToken`).
- Test naming: `MethodName_Scenario_ExpectedOutcome` (e.g., `PlaceOrder_WhenInventoryInsufficient_ThrowsInsufficientStockException`).
- Structure tests as Arrange–Act–Assert.
- Unit tests must be fast and deterministic: no real I/O, network, clock, or `Task.Delay`. Abstract time behind `TimeProvider`.
- Run `dotnet test` after every change and before declaring any work finished.
