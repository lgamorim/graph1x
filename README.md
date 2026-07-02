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
| M4 | BFS/DFS, cycle detection, topological sort | ✅ |
| M5 | Connectivity (components, Tarjan SCC) | ✅ |
| M6 | Shortest paths (Dijkstra, Bellman-Ford, Floyd-Warshall, A*) | ✅ |
| M7 | MST (Kruskal, Prim) | ✅ |
| M8 | Fluent builder, structural queries, dense storage | ✅ |
| M9 | Hypergraph | ✅ |
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

Algorithms live in `Graph1x.Algorithms` as extension methods. Traversals are lazy iterators (implemented without recursion, so deep graphs cannot overflow the stack):

```csharp
using Graph1x.Algorithms;

foreach (var v in graph.BreadthFirstSearch("a")) { /* ... */ }
graph.DepthFirstSearch("a");            // pre-order
graph.DepthFirstSearchPostOrder("a");   // post-order

graph.HasCycle();                       // directed or undirected
graph.FindCycle();                      // the cycle's vertices, or null
graph.TopologicalSort();                // Kahn's algorithm; throws GraphCycleException on cycles
```

Cycle detection understands multigraphs (two parallel undirected edges form a cycle) and self-loops. `GraphCycleException` carries the offending cycle.

Connectivity queries:

```csharp
graph.ConnectedComponents();          // direction-agnostic components
graph.IsConnected();                  // at most one component (empty graph: true)
directed.WeaklyConnectedComponents(); // components after forgetting direction
directed.StronglyConnectedComponents(); // Tarjan, iterative; reverse topological order
```

Shortest paths default to Dijkstra via the facade; the strategies are swappable behind `IShortestPathAlgorithm<,,>`:

```csharp
var route = graph.ShortestPath("LIS", "MAD");            // weighted edges carry the weights
var hops  = graph.ShortestPath("a", "z", _ => 1);        // any edge type + weight selector

route.IsReachable;  // false instead of exceptions for missing routes
route.Distance;     // total weight (throws if unreachable)
route.Path;         // ["LIS", ..., "MAD"]

// Negative weights? Bellman-Ford (throws NegativeCycleException on negative cycles).
new BellmanFordShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight)
    .FindPath(graph, "a", "b");

// All pairs at once (Floyd-Warshall), or heuristic-guided search (A*).
new FloydWarshallAllShortestPaths<string, WeightedEdge<string, int>, int>(e => e.Weight)
    .Compute(graph)
    .Between("a", "b");
new AStarShortestPath<Cell, WeightedEdge<Cell, int>, int>(e => e.Weight, Manhattan)
    .FindPath(grid, start, goal);
```

Dijkstra and A* reject negative weights with `NegativeWeightException` and point you to Bellman-Ford.

Minimum spanning trees (undirected graphs; disconnected input yields a spanning forest):

```csharp
var forest = network.MinimumSpanningForest();       // Kruskal by default
new PrimMinimumSpanningTree<string, WeightedEdge<string, int>, int>(e => e.Weight)
    .FindMinimumSpanningForest(network);            // or Prim, same interface
```

Graphs can be built fluently, and structural queries cover density, degree sequence, bipartiteness, and transpose:

```csharp
using Graph1x.Builders;

var graph = Graph.DirectedWeighted<string, int>()
    .AddEdge("a", "b", 3)
    .AddEdge("b", "c", 4)
    .Build();                     // typed DirectedGraph<string, WeightedEdge<string, int>>

Graph.Wrap(new DirectedAcyclicGraph<string, Edge<string>>()) // build onto any graph
    .AddVertices("a", "b")
    .Build();

graph.Density();                  // E / V(V-1) directed, 2E / V(V-1) undirected
graph.DegreeSequence();           // descending degrees
graph.IsBipartite();              // 2-colorability (direction ignored)
graph.FindBipartition();          // the two vertex sets, or null
graph.Transpose();                // reversed copy of a directed graph
```

For dense graphs, `DirectedAdjacencyMatrixGraph` and `UndirectedAdjacencyMatrixGraph` offer O(1) edge lookup behind the exact same `IMutableGraph` contract (they pass the same contract test suite as the adjacency-list types).

Hypergraphs (edges joining any number of vertices) are a standalone type with their own incidence and connectivity queries:

```csharp
using Graph1x.Hypergraphs;

var teams = new Hypergraph<string>();
var kickoff = teams.AddHyperedge("ana", "bruno", "carla"); // returns a handle
teams.Degree("ana");             // number of incident hyperedges
teams.AreConnected("ana", "dora");
teams.ConnectedComponents();
teams.RemoveHyperedge(kickoff);
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
