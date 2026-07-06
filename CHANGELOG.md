# Changelog

All notable changes to Graph1x are documented in this file. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow
[Semantic Versioning](https://semver.org).

## [Unreleased]

### Added

- Graph set operations in `Graph1x.Algorithms`: `Subgraph` (induced by a
  vertex selection; unknown vertices ignored), `Union` (operands must agree
  on direction; result family and comparer come from the first), and
  `Complement` (simple graphs only; never emits self-loops). Results are new
  graphs matching the source's direction/parallel-edge policy, with
  directed-typed overloads preserving `IDirectedGraph` dispatch.

## [0.5.0] - 2026-07-05

### Changed

- Dev dependencies refreshed to latest: coverlet.collector 10.0.1,
  Microsoft.NET.Test.Sdk 18.7.0, xunit.runner.visualstudio 3.1.5,
  BenchmarkDotNet 0.15.8. The library itself keeps zero package references.

### Added

- DocFX documentation site (API reference generated from the XML docs)
  deployed to GitHub Pages on every master push.
- Node-link JSON serialization: `ToJson` export (Utf8JsonWriter, no
  reflection) and `GraphJson.Parse`/`ParseDirected`/`ParseUndirected`
  (+ weighted variants) importing into lossless multigraph types.
- `CancellationToken` overloads on long-running algorithms (Floyd-Warshall,
  single-source bulk queries, centrality, PageRank, max-flow, transitive
  closure/reduction, condensation, distance metrics), checked cooperatively
  at phase boundaries; `IMaximumFlowAlgorithm` gains a default-implemented
  token overload so existing implementations keep compiling.
- Public API lockdown via Microsoft.CodeAnalysis.PublicApiAnalyzers: the full
  surface is baselined, so any accidental signature change is now a build
  error.
- Trim/Native AOT compatibility (`IsAotCompatible`): analyzers report zero
  findings and a full-trim consumer publish runs correctly.

### Changed

- `PageRank` is now a single method with four optional parameters (the token
  overload merged in), per the Roslyn optional-parameter API guideline; all
  existing call shapes still compile.
- Benchmark-guided allocation reductions: pre-sized collections in BFS/DFS,
  Dijkstra/Bellman-Ford, Tarjan/components, and the flow residual network;
  PageRank caches out-degrees and swaps rank buffers instead of allocating
  per iteration. Typical wins: −18–36% allocations on traversal and
  shortest-path hot paths (A* intentionally left unsized — measurement showed
  pre-sizing hurts it).

## [0.4.0] - 2026-07-05

### Added

- Distance metrics: `Eccentricity`, `Diameter`, `Radius`, `Center`,
  `Periphery`, and `AveragePathLength`, in hop-count and weighted variants;
  connected input required (strongly connected for directed graphs).
- Centrality measures: `DegreeCentrality`, `ClosenessCentrality`
  (Wasserman-Faust scaled), `BetweennessCentrality` (Brandes, hop-count and
  weighted), and `PageRank` with damping, tolerance, and dangling-node
  handling.
- `Condense()`: the SCC condensation DAG with component/member lookups,
  making DAG-only tools applicable to any directed graph.
- GraphML serialization: `ToGraphMl` export (weights, escaping, deterministic
  output) and `GraphMl.Parse`/`ParseDirected`/`ParseUndirected` (+ weighted
  variants) importing into lossless multigraph types.
- `DinicMaximumFlow` behind the existing `IMaximumFlowAlgorithm` interface
  (level graphs + blocking flows), sharing the residual network with
  Edmonds-Karp.
- `bench/Graph1x.Benchmarks`: BenchmarkDotNet suite (storage, traversal,
  shortest paths, Edmonds-Karp vs Dinic, MST, SCC/matching) over seeded
  generator fixtures.

### Changed

- Package author corrected to "Luís Amorim" (matching the git identity).
- Package license expression corrected to Apache-2.0, matching the LICENSE
  change in the repository.

## [0.3.0] - 2026-07-03

### Added

- Tag-triggered release workflow publishing the package and symbols to
  NuGet.org and creating a GitHub Release with notes from this changelog.
- `AsReadOnly()` live read-only views (directed views keep directed dispatch)
  and `ToFrozen()` immutable snapshots safe for concurrent readers.
- Eulerian trails: `HasEulerianCircuit`/`HasEulerianPath` existence checks and
  Hierholzer-based `FindEulerianCircuit`/`FindEulerianPath`, multigraph-aware.
- DSatur vertex coloring (`ColorVertices`) returning a proper coloring and a
  chromatic-number upper bound; exact on bipartite graphs.
- Hypergraph expansions: `ToCliqueExpansion` (2-section) and the lossless
  `ToBipartiteIncidenceGraph`, making the whole binary-edge algorithm suite
  usable on hypergraph data.
- `GraphGenerator`: deterministic seeded Erdős–Rényi (directed/undirected),
  `Complete`, `CompleteBipartite`, `Path`, `Cycle`, `Star`, and `Grid`
  generators over integer vertices.

## [0.2.0] - 2026-07-03

### Added

- CI on GitHub Actions (Linux + Windows, .NET 8 + .NET 10), Source Link,
  snupkg symbol packages, deterministic CI builds.
- Edmonds-Karp maximum flow with per-edge flows and a certifying minimum cut.
- Hopcroft-Karp maximum bipartite matching.
- Bridges and articulation points (shared iterative low-link DFS).
- Transitive closure and DAG-only transitive reduction.
- Graphviz DOT export with escaping and label selectors.
- Single-source shortest paths: `FindPathsFrom` on Dijkstra/Bellman-Ford and
  the `ShortestPathsFrom` facade.

### Fixed

- Test project `RollForward` policy (`LatestMajor` → `Major`) so the matching
  runtime is used when installed.

## [0.1.0] - 2026-07-02

### Added

- Graph types: `DirectedGraph`, `UndirectedGraph`, `DirectedMultigraph`,
  `UndirectedMultigraph`, `DirectedAcyclicGraph`,
  `DirectedAdjacencyMatrixGraph`, `UndirectedAdjacencyMatrixGraph`, and a
  standalone `Hypergraph`.
- Edge types `Edge` and `WeightedEdge` with generic-math (`INumber`) weights.
- Algorithms: BFS/DFS (lazy, iterative), cycle detection, topological sort,
  connected/weakly connected components, Tarjan SCC, Dijkstra, Bellman-Ford,
  Floyd-Warshall, A*, Kruskal, Prim, density, degree sequence, bipartiteness,
  and transpose.
- Fluent `GraphBuilder`, `net8.0` + `net10.0` multi-targeting, NuGet package
  metadata.
