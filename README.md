# Graph1x

A .NET library for creating, mutating, and analyzing graphs, built with modern C# and developed test-first.

## Goals

Graph1x aims to cover the standard graph taxonomy under one coherent, strongly-typed API:

- **Direction** — directed and undirected graphs
- **Weight** — weighted and unweighted edges (weights via C# generic math, any `INumber<T>`)
- **Cycles** — cyclic graphs and DAG-enforcing types that reject cycle-forming edges
- **Structure/density** — adjacency-list (sparse) and adjacency-matrix (dense) storage behind the same contract
- **Special structures** — multigraphs (parallel edges) and a standalone hypergraph type

On top of the data structures, the library ships the classic algorithm suite: BFS/DFS, cycle detection, topological sort, connected/strongly connected components, shortest paths (Dijkstra, Bellman-Ford, Floyd-Warshall, A*), minimum spanning trees (Kruskal, Prim), and structural queries (degree, density, bipartiteness, transpose).

## Status

Under active development, milestone by milestone (TDD — tests are written before the implementation):

| Milestone | Contents | Status |
|---|---|---|
| M0 | Project scaffolding, build props, analyzers | ✅ |
| M1 | Edge types and core graph abstractions | — |
| M2 | `UndirectedGraph`, `DirectedGraph` | — |
| M3 | Multigraphs, `DirectedAcyclicGraph` | — |
| M4 | BFS/DFS, cycle detection, topological sort | — |
| M5 | Connectivity (components, Tarjan SCC) | — |
| M6 | Shortest paths (Dijkstra, Bellman-Ford, Floyd-Warshall, A*) | — |
| M7 | MST (Kruskal, Prim) | — |
| M8 | Fluent builder, structural queries, dense storage | — |
| M9 | Hypergraph | — |
| M10 | Docs and packaging | — |

## Building

```
dotnet build Graph1x.sln
dotnet test Graph1x.sln
```

Requires the .NET 10 SDK. Warnings are treated as errors and .NET analyzers run at the latest analysis level.

## Project layout

- `src/Graph1x` — the library
- `test/Graph1x.UnitTests` — xUnit v3 test suite
