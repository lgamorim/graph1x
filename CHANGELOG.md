# Changelog

All notable changes to Graph1x are documented in this file. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow
[Semantic Versioning](https://semver.org).

## [Unreleased]

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
