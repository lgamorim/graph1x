# Graph1x

[![CI](https://github.com/lgamorim/graph1x/actions/workflows/ci.yml/badge.svg)](https://github.com/lgamorim/graph1x/actions/workflows/ci.yml)
[![Docs](https://github.com/lgamorim/graph1x/actions/workflows/docs.yml/badge.svg)](https://lgamorim.github.io/graph1x/)

**[API documentation →](https://lgamorim.github.io/graph1x/)**

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

Built milestone by milestone with TDD (tests written before the implementation); 600+ unit tests cover the edge cases, including a shared contract suite that every graph implementation must pass. CI runs the full suite on Linux and Windows against both target frameworks, and the package ships Source Link with a symbols package for debugging.

| Area | Contents |
|---|---|
| Graph types | `DirectedGraph`, `UndirectedGraph`, `DirectedMultigraph`, `UndirectedMultigraph`, `DirectedAcyclicGraph`, `DirectedAdjacencyMatrixGraph`, `UndirectedAdjacencyMatrixGraph`, `Hypergraph` |
| Traversal | BFS, DFS pre/post-order (lazy, iterative) |
| Cycles | `HasCycle`/`FindCycle`, Kahn topological sort |
| Eulerian trails | `HasEulerianCircuit`/`Path`, Hierholzer `FindEulerianCircuit`/`Path` |
| Connectivity | Connected/weakly connected components, Tarjan SCC, condensation, bridges, articulation points |
| Shortest paths | Dijkstra, Bellman-Ford, Floyd-Warshall, A* |
| Spanning trees | Kruskal, Prim (forests on disconnected input) |
| Flow networks | Edmonds-Karp and Dinic maximum flow with certifying minimum cut |
| Matching | Hopcroft-Karp maximum bipartite matching |
| Structure | Density, degree sequence, bipartiteness, transpose, transitive closure/reduction |
| Coloring | DSatur heuristic (`ColorVertices`), exact on bipartite graphs |
| Distance metrics | Eccentricity, diameter, radius, center/periphery, average path length |
| Centrality | Degree, closeness (Wasserman-Faust), Brandes betweenness, PageRank |
| Construction | Fluent `GraphBuilder` with typed `Build()` |
| Views | `AsReadOnly()` live views, `ToFrozen()` immutable snapshots |
| Serialization | Graphviz DOT export; GraphML and node-link JSON round-trips |
| Generators | Seeded Erdős–Rényi, complete, bipartite, path, cycle, star, grid |

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
graph.FindEulerianCircuit();            // every edge exactly once, or null (Hierholzer)
graph.FindEulerianPath();               // Königsberg says null
```

Cycle detection understands multigraphs (two parallel undirected edges form a cycle) and self-loops. `GraphCycleException` carries the offending cycle.

Connectivity queries:

```csharp
graph.ConnectedComponents();          // direction-agnostic components
graph.IsConnected();                  // at most one component (empty graph: true)
directed.WeaklyConnectedComponents(); // components after forgetting direction
directed.StronglyConnectedComponents(); // Tarjan, iterative; reverse topological order

var condensation = directed.Condense(); // each SCC collapsed to one vertex
condensation.Graph.TopologicalSort();   // the condensation is always a DAG
condensation.ComponentOf("a");          // vertex -> component index
condensation.Members(0);                // component index -> original vertices
```

Shortest paths default to Dijkstra via the facade; the strategies are swappable behind `IShortestPathAlgorithm<,,>`:

```csharp
var route = graph.ShortestPath("LIS", "MAD");            // weighted edges carry the weights
var hops  = graph.ShortestPath("a", "z", _ => 1);        // any edge type + weight selector

route.IsReachable;  // false instead of exceptions for missing routes
route.Distance;     // total weight (throws if unreachable)
route.Path;         // ["LIS", ..., "MAD"]

// Querying many targets from one source? One run, many lookups:
var fromLisbon = graph.ShortestPathsFrom("LIS");
fromLisbon.To("MAD");     // ShortestPathResult, no recomputation
fromLisbon.Distances;     // every reachable vertex at once

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

Maximum flow (directed networks, non-negative capacities) returns the flow value, per-edge flows, and a minimum cut that certifies optimality:

```csharp
var result = network.MaximumFlow("source", "sink");   // Edmonds-Karp by default
network.MaximumFlow("s", "t", e => e.Capacity);       // or any capacity selector
new DinicMaximumFlow<string, WeightedEdge<string, int>, int>(e => e.Weight)
    .FindMaximumFlow(network, "s", "t");              // Dinic for large/dense networks

result.FlowValue;           // max flow == min cut capacity
result.EdgeFlows;           // flow per edge (parallel edges listed individually)
result.MinCutEdges;         // the bottleneck edges
result.SourceSideOfMinCut;  // the residual-reachable vertex set
```

Maximum bipartite matching (undirected bipartite graphs; the partition is derived automatically):

```csharp
var pairs = graph.MaximumBipartiteMatching(); // Hopcroft-Karp, O(E·√V)
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
graph.FindBridges();              // edges whose removal disconnects (undirected)
graph.FindArticulationPoints();   // cut vertices (undirected)
dag.TransitiveClosure();          // u->v for every non-empty path; cycles gain self-loops
dag.TransitiveReduction();        // minimal edge set with the same reachability (DAGs only)

var coloring = graph.ColorVertices();  // DSatur; ColorCount bounds the chromatic number
coloring.ColorOf("a");                 // 0-based color, adjacent vertices always differ

graph.Diameter();                 // longest shortest path (hops, or pass a weight selector)
graph.Radius();                   // smallest eccentricity
graph.Center();                   // vertices at eccentricity == radius
graph.AveragePathLength();        // mean distance over ordered pairs
```

Distance metrics require a connected graph (strongly connected when directed) and throw `InvalidOperationException` otherwise — no sentinel infinities.

Long-running computations (all-pairs paths, centrality, PageRank, flows, closures, metrics, condensation) accept a `CancellationToken` via additive overloads, checked cooperatively at phase boundaries:

```csharp
graph.BetweennessCentrality(cancellationToken);
network.MaximumFlow(s, t, e => e.Capacity, cancellationToken);
```

Centrality measures answer "which vertices matter":

```csharp
graph.DegreeCentrality();          // degree / (V-1)
graph.ClosenessCentrality();       // Wasserman-Faust scaled; disconnected graphs fine
graph.BetweennessCentrality();     // Brandes; weighted overload takes a selector
network.PageRank(damping: 0.85);   // directed; ranks sum to 1, dangling nodes handled
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

// Expansions bridge hypergraphs into the full algorithm suite:
teams.ToCliqueExpansion();          // co-membership graph (2-section)
teams.ToBipartiteIncidenceGraph();  // lossless vertex/hyperedge bipartite graph
```

Ready-made structures for tests, demos, and benchmarks come from `GraphGenerator` (`ErdosRenyi(n, p, seed)`, `Complete(n)`, `Grid(w, h)`, …) — seeded, so results are reproducible.

Hand out graphs without handing out mutation — live views and immutable snapshots both stay fully algorithm-compatible (directed views keep directed dispatch):

```csharp
IReadOnlyGraph<string, Edge<string>> view = graph.AsReadOnly(); // live, not castable to IMutableGraph
var snapshot = graph.ToFrozen();                                // deep copy, safe for concurrent readers
```

Any graph renders to Graphviz DOT for quick visualization (`dot -Tsvg`):

```csharp
using Graph1x.Serialization;

var dot = graph.ToDot();                                  // digraph/graph picked automatically
graph.ToDot(new DotExportOptions<string, WeightedEdge<string, int>>
{
    GraphName = "network",
    EdgeLabel = e => e.Weight.ToString(),                 // [label="…"] per edge
});
```

GraphML round-trips for persistence and interop with other tools:

```csharp
var xml = graph.ToGraphMl();                    // weights via GraphMlExportOptions.EdgeWeight
var restored = GraphMl.Parse(xml);              // direction auto-detected from edgedefault
GraphMl.ParseDirectedWeighted(xml);             // typed weighted variants
```

JSON uses the node-link shape shared with NetworkX/D3 (`{ "directed": …, "nodes": […], "edges": […] }`), written without reflection:

```csharp
var json = graph.ToJson();                      // weights via GraphJsonExportOptions.EdgeWeight
GraphJson.Parse(json);                          // direction auto-detected
GraphJson.ParseUndirectedWeighted(json);        // typed weighted variants
```

## Building

```
dotnet build Graph1x.sln
dotnet test Graph1x.sln
```

The library targets **.NET 8 (LTS)** and **.NET 10**; the test suite runs against both. Building requires the .NET 10 SDK. Warnings are treated as errors and .NET analyzers run at the latest analysis level.

## Performance

Selected numbers from the BenchmarkDotNet suite (ShortRun job, .NET 10, single dev machine — run `bench/Graph1x.Benchmarks` yourself for rigorous figures):

| Scenario | Result |
|---|---|
| `ShortestPathsFrom` vs one `ShortestPath` per target (50×50 grid, 2 500 targets) | ~0.5 ms vs ~554 ms — **~1 100× faster**, ~1 100× fewer allocations |
| A* (Manhattan) vs Dijkstra, corner to corner on the same grid | ~30 µs vs ~435 µs — **~14× faster** |
| Edmonds-Karp vs Dinic (random networks, 50–150 vertices) | EK slightly ahead at these sizes (Dinic ratio 1.03–1.22×) — Dinic's level-graph overhead pays off on larger/denser networks |

## Releasing

Pushing a `v*` tag (e.g. `v0.3.0`) runs the release workflow: build, test, pack, publish to NuGet.org (requires the `NUGET_API_KEY` repository secret), and create a GitHub Release with notes from [CHANGELOG.md](CHANGELOG.md). Running the workflow manually performs a dry run that stops after packing.

## Project layout

- `src/Graph1x` — the library
- `test/Graph1x.UnitTests` — xUnit v3 test suite
- `bench/Graph1x.Benchmarks` — BenchmarkDotNet suite (`dotnet run -c Release --project bench/Graph1x.Benchmarks`; seeded `GraphGenerator` fixtures, `--job Dry` for a smoke run)
