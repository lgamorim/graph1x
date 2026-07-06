# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Graph1x is a .NET graph library (directed/undirected, weighted via generic math, multigraphs, DAGs,
adjacency-matrix/dense variants, hypergraphs) plus the classic algorithm suite (traversal, cycles,
shortest paths, MST, max flow, matching, centrality, clustering, serialization). Developed test-first,
milestone by milestone — see the commit log (`M<n>` suffixes) and [CHANGELOG.md](CHANGELOG.md) for history.

## Build / test / bench commands

```
dotnet build Graph1x.sln
dotnet test Graph1x.sln
dotnet test Graph1x.sln --filter "FullyQualifiedName~DijkstraTests"   # single test class
dotnet pack src/Graph1x/Graph1x.csproj -c Release
dotnet run -c Release --project bench/Graph1x.Benchmarks -- --job Dry  # smoke-test a benchmark
```

- Requires the **.NET 10 SDK**; the library and tests target **net8.0 and net10.0** both, and CI
  (`.github/workflows/ci.yml`) runs the full matrix on Linux and Windows for both TFMs.
- Warnings are treated as errors (`TreatWarningsAsErrors`) with analyzers at `AnalysisLevel=latest`
  (`Directory.Build.props`) — a build with any analyzer warning fails, so fix warnings rather than
  suppress them.
- Test project uses **xUnit v3**. `TestContext.Current.CancellationToken` is expected on
  token-accepting calls (xUnit1051) except in the cancellation test suite, which intentionally passes
  specific tokens — that's why `xUnit1051` is suppressed project-wide.

## Cross-cutting conventions (apply these without being asked)

- **Collection idiom, not exceptions**: `AddVertex`/`AddEdge`/`RemoveVertex`/`RemoveEdge` return `bool`
  for duplicate/missing cases. Lookup failures on shortest paths use `IsReachable` sentinels rather
  than throwing. Reserve exceptions for programmer errors (null args, vertex not in graph) and named
  domain exceptions (`GraphCycleException`, `NegativeCycleException`, `NegativeWeightException`).
- **Traversals are lazy, iterative generators** — implemented without recursion so deep graphs can't
  stack-overflow. Preserve this when touching `GraphTraversalExtensions`/`GraphTraversalCore`.
- **`CancellationToken` overloads** are additive on long-running algorithms (all-pairs paths,
  centrality, PageRank, flows, closures, distance metrics), checked cooperatively at phase boundaries
  — not per-iteration. Per-source analyses (betweenness, closeness, distance metrics) also get a
  `ParallelOptions` overload; the sequential path stays as the reference implementation and parallel
  results must match it (bit-identical for per-vertex results, floating-point-equivalent for
  accumulated ones like betweenness). Follow this pattern for new long-running algorithms.
- **Shared contract test suites**: `GraphContractTests`, `SimpleGraphContractTests`,
  `MultigraphContractTests` in `test/Graph1x.UnitTests` are abstract base classes that every graph
  implementation's test class inherits, so a new graph type must pass the same behavioral contract as
  every existing one. When adding a graph type, add it to the relevant contract suite(s) rather than
  writing one-off tests.
- **Public API is analyzer-locked** (`Microsoft.CodeAnalysis.PublicApiAnalyzers`): any public surface
  change requires updating `src/Graph1x/PublicAPI.Unshipped.txt` (new members) and, at release time,
  moving shipped entries into `PublicAPI.Shipped.txt` — the build fails otherwise.
- **Strong naming**: `Graph1x.snk` is committed and used to sign both the library and the test
  assembly (so `InternalsVisibleTo` keeps working). Never regenerate `Graph1x.snk` — treat it as fixed
  project identity, not a security boundary.
- Nullable reference types, implicit usings, and `IsAotCompatible=true` (reflection-free) apply
  library-wide; don't introduce reflection-based code paths in `src/Graph1x`.

## Release process

Version lives in `src/Graph1x/Graph1x.csproj` (`<Version>`); notes live in [CHANGELOG.md](CHANGELOG.md)
under `## [x.y.z] - date`, following Keep a Changelog / SemVer. Pushing a `v*` tag runs
`.github/workflows/release.yml`: build, test, pack, extract that version's section from
`CHANGELOG.md`, publish to NuGet.org, and create a GitHub Release from the package. Manual dispatch of
the workflow performs the same steps as a dry run and stops after packing (no publish/release).
