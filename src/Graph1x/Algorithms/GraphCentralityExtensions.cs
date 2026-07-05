using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Centrality measures: degree, closeness (Wasserman-Faust scaled, so
/// disconnected graphs need no special casing), betweenness via Brandes'
/// algorithm (breadth-first for hop counts, Dijkstra-based for weights), and
/// PageRank for directed graphs. On multigraphs, parallel edges count as
/// distinct shortest paths, which is the natural multigraph semantics.
/// </summary>
public static class GraphCentralityExtensions
{
    /// <summary>Gets each vertex's degree divided by (V - 1); on directed graphs the total degree (in + out) is used.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Degree centrality per vertex (0 for a single-vertex graph).</returns>
    public static IReadOnlyDictionary<TVertex, double> DegreeCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        var scale = graph.VertexCount > 1 ? 1.0 / (graph.VertexCount - 1) : 0.0;
        var centrality = new Dictionary<TVertex, double>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            centrality[vertex] = graph.Degree(vertex) * scale;
        }

        return centrality;
    }

    /// <summary>Gets hop-count closeness centrality.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Closeness per vertex.</returns>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ClosenessCentrality(_ => 1);

    /// <summary>
    /// Gets closeness centrality from weighted distances (measured from each
    /// vertex outward), Wasserman-Faust scaled by reachable-set size so
    /// disconnected graphs yield comparable values; unreachable and isolated
    /// vertices score 0.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>Closeness per vertex.</returns>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);

        var centrality = new Dictionary<TVertex, double>(graph.VertexComparer);
        var vertexCount = graph.VertexCount;
        foreach (var vertex in graph.Vertices)
        {
            var distances = graph.ShortestPathsFrom(vertex, weightSelector).Distances;
            var reachable = distances.Count; // includes the vertex itself at distance zero
            var total = distances.Values.Sum(double.CreateChecked);
            centrality[vertex] = reachable > 1 && total > 0
                ? (reachable - 1.0) / (vertexCount - 1.0) * ((reachable - 1.0) / total)
                : 0.0;
        }

        return centrality;
    }

    /// <summary>Gets hop-count betweenness centrality (Brandes, breadth-first).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return BrandesAccumulate(graph, BreadthFirstStage);

        (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)
            BreadthFirstStage(TVertex source)
        {
            var comparer = graph.VertexComparer;
            var distance = new Dictionary<TVertex, int>(comparer) { [source] = 0 };
            var sigma = new Dictionary<TVertex, double>(comparer) { [source] = 1.0 };
            var predecessors = new Dictionary<TVertex, List<TVertex>>(comparer) { [source] = [] };
            var order = new List<TVertex>();
            var queue = new Queue<TVertex>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                order.Add(current);
                foreach (var (neighbor, _) in GraphTraversalCore.OutgoingArcs(graph, current))
                {
                    if (!distance.TryGetValue(neighbor, out var known))
                    {
                        distance[neighbor] = distance[current] + 1;
                        sigma[neighbor] = 0.0;
                        predecessors[neighbor] = [];
                        queue.Enqueue(neighbor);
                        known = distance[neighbor];
                    }

                    if (known == distance[current] + 1)
                    {
                        sigma[neighbor] += sigma[current];
                        predecessors[neighbor].Add(current);
                    }
                }
            }

            return (order, sigma, predecessors);
        }
    }

    /// <summary>Gets weighted betweenness centrality (Brandes over Dijkstra).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        return BrandesAccumulate(graph, DijkstraStage);

        (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)
            DijkstraStage(TVertex source)
        {
            var comparer = graph.VertexComparer;
            var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
            var sigma = new Dictionary<TVertex, double>(comparer) { [source] = 1.0 };
            var predecessors = new Dictionary<TVertex, List<TVertex>>(comparer) { [source] = [] };
            var order = new List<TVertex>();
            var settled = new HashSet<TVertex>(comparer);
            var frontier = new PriorityQueue<TVertex, TWeight>();
            frontier.Enqueue(source, TWeight.Zero);

            while (frontier.TryDequeue(out var current, out _))
            {
                if (!settled.Add(current))
                {
                    continue;
                }

                order.Add(current);
                foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
                {
                    var weight = weightSelector(edge);
                    if (weight < TWeight.Zero)
                    {
                        throw new NegativeWeightException(
                            $"Edge '{edge}' has negative weight {weight}; betweenness centrality requires non-negative weights.");
                    }

                    if (settled.Contains(neighbor))
                    {
                        continue;
                    }

                    var candidate = distance[current] + weight;
                    if (!distance.TryGetValue(neighbor, out var known) || candidate < known)
                    {
                        distance[neighbor] = candidate;
                        sigma[neighbor] = sigma[current];
                        predecessors[neighbor] = [current];
                        frontier.Enqueue(neighbor, candidate);
                    }
                    else if (candidate == known)
                    {
                        sigma[neighbor] += sigma[current];
                        predecessors[neighbor].Add(current);
                    }
                }
            }

            return (order, sigma, predecessors);
        }
    }

    /// <summary>
    /// Computes PageRank by power iteration with uniform teleportation;
    /// dangling-vertex mass is redistributed uniformly, so ranks always sum
    /// to one. Parallel edges each carry their share of the source's rank.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to rank.</param>
    /// <param name="damping">The damping factor in [0, 1]; 0.85 is the classic choice.</param>
    /// <param name="maxIterations">The iteration cap, at least 1.</param>
    /// <param name="tolerance">The L1 convergence threshold.</param>
    /// <returns>PageRank per vertex, summing to 1 (empty for the empty graph).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Damping leaves [0, 1] or <paramref name="maxIterations"/> is below 1.</exception>
    public static IReadOnlyDictionary<TVertex, double> PageRank<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        double damping = 0.85,
        int maxIterations = 100,
        double tolerance = 1e-9)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentOutOfRangeException.ThrowIfNegative(damping);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(damping, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);

        var count = graph.VertexCount;
        var ranks = new Dictionary<TVertex, double>(graph.VertexComparer);
        if (count == 0)
        {
            return ranks;
        }

        foreach (var vertex in graph.Vertices)
        {
            ranks[vertex] = 1.0 / count;
        }

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var danglingMass = graph.Vertices
                .Where(vertex => graph.OutDegree(vertex) == 0)
                .Sum(vertex => ranks[vertex]);

            var next = new Dictionary<TVertex, double>(graph.VertexComparer);
            var change = 0.0;
            foreach (var vertex in graph.Vertices)
            {
                var incoming = graph.InEdges(vertex)
                    .Sum(edge => ranks[edge.Source] / graph.OutDegree(edge.Source));
                var rank = ((1.0 - damping) / count) + (damping * (incoming + (danglingMass / count)));
                next[vertex] = rank;
                change += Math.Abs(rank - ranks[vertex]);
            }

            ranks = next;
            if (change < tolerance)
            {
                break;
            }
        }

        return ranks;
    }

    /// <summary>
    /// The shared Brandes dependency-accumulation phase: walk vertices in
    /// reverse settlement order, pushing each vertex's dependency onto its
    /// shortest-path predecessors. Undirected results are halved because every
    /// unordered pair is visited from both endpoints.
    /// </summary>
    private static Dictionary<TVertex, double> BrandesAccumulate<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TVertex, (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)> stage)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var centrality = new Dictionary<TVertex, double>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            centrality[vertex] = 0.0;
        }

        foreach (var source in graph.Vertices)
        {
            var (order, sigma, predecessors) = stage(source);
            var dependency = new Dictionary<TVertex, double>(comparer);
            foreach (var vertex in order)
            {
                dependency[vertex] = 0.0;
            }

            for (var i = order.Count - 1; i >= 0; i--)
            {
                var vertex = order[i];
                foreach (var predecessor in predecessors[vertex])
                {
                    dependency[predecessor] += sigma[predecessor] / sigma[vertex] * (1.0 + dependency[vertex]);
                }

                if (!comparer.Equals(vertex, source))
                {
                    centrality[vertex] += dependency[vertex];
                }
            }
        }

        if (!graph.IsDirected)
        {
            foreach (var vertex in centrality.Keys.ToList())
            {
                centrality[vertex] /= 2.0;
            }
        }

        return centrality;
    }
}
