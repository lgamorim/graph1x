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
| M1 | Edge types and core graph abstractions | ✅ |
| M2 | `UndirectedGraph`, `DirectedGraph` | ✅ |
| M3 | Multigraphs, `DirectedAcyclicGraph` | ✅ |
| M4 | BFS/DFS, cycle detection, topological sort | — |
| M5 | Connectivity (components, Tarjan SCC) | — |
| M6 | Shortest paths (Dijkstra, Bellman-Ford, Floyd-Warshall, A*) | — |
| M7 | MST (Kruskal, Prim) | — |
| M8 | Fluent builder, structural queries, dense storage | — |
| M9 | Hypergraph | — |
| M10 | Docs and packaging | — |

## Usage

Edges are lightweight value types; weighted edges accept any numeric type via generic math:

```csharp
using Graph1x.Edges;

var road = new Edge<string>("Lisbon", "Porto");
var toll = new WeightedEdge<string, decimal>("Lisbon", "Porto", 22.85m);

var (source, target, weight) = toll; // deconstruction
```

Edge values are ordered pairs — undirected semantics (`a-b` == `b-a`) are applied by the graph that stores them, not by the edge itself.

Graphs are mutable adjacency-list structures. Add/Remove follow the .NET collection idiom (`bool` instead of exceptions), `AddEdge` auto-adds missing endpoint vertices, and `RemoveVertex` cascades to incident edges:

```csharp
using Graph1x;
using Graph1x.Edges;

var graph = new DirectedGraph<string, Edge<string>>();
graph.AddEdge(new Edge<string>("a", "b"));
graph.AddEdge(new Edge<string>("b", "c"));

graph.ContainsEdge("a", "b");   // true
graph.ContainsEdge("b", "a");   // false — direction matters
graph.OutDegree("b");           // 1
graph.RemoveVertex("b");        // also removes a->b and b->c

// Undirected graphs treat endpoints symmetrically and accept custom comparers.
var roads = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
roads.AddEdge(new Edge<string>("Lisbon", "Porto"));
roads.ContainsEdge("PORTO", "lisbon"); // true
```

Self-loops are allowed everywhere except in DAGs (an undirected self-loop counts 2 toward the degree; a directed one counts 1 in + 1 out).

Multigraphs accept parallel edges; DAGs reject anything that would create a cycle:

```csharp
var flights = new DirectedMultigraph<string, WeightedEdge<string, decimal>>();
flights.AddEdge(new WeightedEdge<string, decimal>("LIS", "OPO", 49.90m));
flights.AddEdge(new WeightedEdge<string, decimal>("LIS", "OPO", 89.90m)); // parallel — allowed
flights.GetEdges("LIS", "OPO");    // both fares

var build = new DirectedAcyclicGraph<string, Edge<string>>();
build.AddEdge(new Edge<string>("compile", "test"));
build.AddEdge(new Edge<string>("test", "package"));
build.AddEdge(new Edge<string>("package", "compile")); // false — would close a cycle
```

## Building

```
dotnet build Graph1x.sln
dotnet test Graph1x.sln
```

Requires the .NET 10 SDK. Warnings are treated as errors and .NET analyzers run at the latest analysis level.

## Project layout

- `src/Graph1x` — the library
- `test/Graph1x.UnitTests` — xUnit v3 test suite
