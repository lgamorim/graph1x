# Changelog

All notable changes to Graph1x are documented in this file. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow
[Semantic Versioning](https://semver.org).

## [Unreleased]

## [1.0.1] - 2026-07-15

Minor fixes found after the 1.0.0 release. No API changes.

### Fixed

- The NuGet.org package readme now renders the logo: the image is a Markdown
  reference (NuGet strips the bare `<img>` HTML) and the package ships a
  dedicated `icon.png` rather than reusing `logo.png`.
- The documentation site shows the Graph1x logo in the navbar instead of the
  default DocFX logo.

### Changed

- Both READMEs gained CI and coverage badges and a "How this was built"
  disclosure; CI now publishes coverage to Codecov.

## [1.0.0] - 2026-07-07

First stable release. The public API is identical to 1.0.0-rc.1 (and to the
surface shipped in 0.7.0): zero API delta through the release candidate.

From this release, versioning follows SemVer strictly: breaking changes to
the public API only in a new major version; additions in minor versions;
fixes in patch versions. The API surface is analyzer-locked
(Microsoft.CodeAnalysis.PublicApiAnalyzers), so any accidental change is a
build error.

## [1.0.0-rc.1] - 2026-07-06

Release candidate for 1.0.0. The public API surface shipped in 0.7.0 is
frozen: no API changes between this candidate and 1.0.0.

### Changed

- The LICENSE copyright notice names the copyright holder (Copyright 2026
  Luís Amorim) instead of the stock Apache 2.0 placeholder, and the README
  shows the Apache 2.0 license badge and a License section.
- The release workflow marks SemVer prerelease tags (a `-` after the
  version, like this candidate) as prereleases on the GitHub Release.

## [0.7.0] - 2026-07-06

### Added

- Maximal clique enumeration: `EnumerateMaximalCliques` (Bron–Kerbosch with
  Tomita pivoting), a lazy iterative generator so the caller controls the
  cost of the exponential worst case by how far it enumerates. Direction is
  ignored, self-loops never count, and multigraph neighbors count once —
  the clustering-coefficient conventions. Deterministic order.

- Mermaid flowchart export: `ToMermaid` with `MermaidExportOptions`
  (vertex/edge label selectors and flow `Direction`). Nodes get synthetic
  insertion-order identifiers (`v0`, `v1`, …) with the display label attached
  at declaration, so arbitrary vertex strings are safe; labels use Mermaid
  entity escaping. Output is deterministic, like the DOT exporter.

## [0.6.0] - 2026-07-06

### Fixed

- `GraphGenerator.CompleteBipartite` now rejects a negative right-side count
  with `ArgumentOutOfRangeException` as documented, instead of silently
  producing a smaller edgeless graph when the two counts summed to a
  non-negative total.
- `GraphDocument.EdgeData` documentation no longer claims document order
  always matches the graph's edge enumeration order: multigraphs enumerate
  edges grouped by source vertex, so foreign documents with interleaved
  edge sources must be correlated by document position. The exporter
  documentation now states the actual (still deterministic) edge order.

### Changed

- The assembly is strong-name signed from this release (the key ships in
  the repository — strong naming is identity, not security). .NET 8+
  ignores strong names for loading, but strongly-named consumers can now
  reference the package.

### Added

- Parallel analysis overloads taking `System.Threading.Tasks.ParallelOptions`
  (degree of parallelism + cancellation, composing with the 0.5.0 token
  design) on the per-source algorithms: `BetweennessCentrality` (hop-count
  and weighted), `ClosenessCentrality`, `Diameter`, `Radius`, `Center`,
  `Periphery`, and `AveragePathLength`. Sequential paths are unchanged and
  remain the reference implementations; per-vertex results (closeness,
  eccentricity-based metrics) are bit-identical to sequential, accumulated
  ones (betweenness) differ only by floating-point merge order. Benchmarks
  compare both paths.

- Attribute round-trips for GraphML and node-link JSON: one
  `GraphAttribute<T>` declaration (typed factories: String/Bool/Int/Long/
  Float/Double) drives both exporters via `VertexAttributes`/
  `EdgeAttributes` on the existing option records; `GraphMl.ParseDocument`
  and `GraphJson.ParseDocument` return a `GraphDocument` carrying the graph
  plus typed per-vertex and per-edge attribute data. Existing `Parse*`
  entry points are unchanged; the JSON path stays reflection-free.

- Random-graph generators with realistic structure: `BarabasiAlbert`
  (preferential attachment; connected, simple, exactly m·(n−m) edges) and
  `WattsStrogatz` (ring lattice with in-place rewiring, so the edge count
  is always n·k/2). Seeded and deterministic like the existing generators.

- Clustering coefficients: `LocalClusteringCoefficient`,
  `ClusteringCoefficients`, `AverageClusteringCoefficient`, and
  `GlobalClusteringCoefficient` (transitivity). Direction is ignored,
  self-loops never count, and multigraph neighbors count once.
- Spectral centrality: `EigenvectorCentrality` (shifted power iteration,
  so bipartite graphs cannot oscillate) and `KatzCentrality`
  (attenuation `alpha` + base `beta`; the DAG-safe alternative), both
  L2-normalized with PageRank-style optional parameters and cancellation.

- DAG path algorithms via single-pass topological relaxation:
  `DagShortestPathsFrom`, `DagLongestPathsFrom` (both reuse
  `SingleSourceShortestPaths`), and `CriticalPath` (the maximum-weight path
  anywhere in the DAG). Negative weights are supported; cyclic input throws
  `GraphCycleException`.

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
