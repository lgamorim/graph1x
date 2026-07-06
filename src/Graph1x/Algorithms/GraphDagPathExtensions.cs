using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Shortest, longest, and critical paths on directed acyclic graphs: one
/// topological pass with edge relaxation, so negative weights are fine (this
/// is the fast answer when Dijkstra rejects them with
/// <see cref="NegativeWeightException"/>). Cyclic input throws
/// <see cref="GraphCycleException"/>.
/// </summary>
public static class GraphDagPathExtensions
{
    /// <summary>
    /// Computes shortest paths from <paramref name="source"/> to every
    /// reachable vertex of a directed acyclic graph, using
    /// <paramref name="weightSelector"/> to read edge weights. Negative
    /// weights are supported.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>A queryable single-source result.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> DagShortestPathsFrom<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.RelaxFrom(source, weightSelector, longest: false);

    /// <summary>
    /// Computes shortest paths from <paramref name="source"/> using the
    /// weights carried by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <returns>A queryable single-source result.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> DagShortestPathsFrom<TVertex, TWeight>(
        this IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.DagShortestPathsFrom(source, edge => edge.Weight);

    /// <summary>
    /// Computes longest paths from <paramref name="source"/> to every
    /// reachable vertex of a directed acyclic graph, using
    /// <paramref name="weightSelector"/> to read edge weights.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>A queryable single-source result carrying longest distances.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> DagLongestPathsFrom<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.RelaxFrom(source, weightSelector, longest: true);

    /// <summary>
    /// Computes longest paths from <paramref name="source"/> using the
    /// weights carried by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <returns>A queryable single-source result carrying longest distances.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> DagLongestPathsFrom<TVertex, TWeight>(
        this IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.DagLongestPathsFrom(source, edge => edge.Weight);

    /// <summary>
    /// Finds the critical path: the maximum-weight path anywhere in the
    /// directed acyclic graph (start and end are free). When every edge is
    /// negative the best path is a single vertex with distance zero.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The heaviest path, from its start vertex to its end vertex.</returns>
    /// <exception cref="InvalidOperationException">The graph has no vertices.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static ShortestPathResult<TVertex, TWeight> CriticalPath<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        if (graph.VertexCount == 0)
        {
            throw new InvalidOperationException("The critical path of an empty graph is undefined.");
        }

        var order = graph.TopologicalSort();
        var comparer = graph.VertexComparer;

        // Every vertex is a valid path start, so every vertex seeds at zero.
        var distance = new Dictionary<TVertex, TWeight>(graph.VertexCount, comparer);
        foreach (var vertex in order)
        {
            distance[vertex] = TWeight.Zero;
        }

        var predecessor = new Dictionary<TVertex, TVertex>(comparer);
        Relax(graph, order, weightSelector, longest: true, distance, predecessor);

        var end = order[0];
        foreach (var vertex in order)
        {
            if (distance[vertex] > distance[end])
            {
                end = vertex;
            }
        }

        var path = new List<TVertex> { end };
        var current = end;
        while (predecessor.TryGetValue(current, out var previous))
        {
            path.Add(previous);
            current = previous;
        }

        path.Reverse();
        return new ShortestPathResult<TVertex, TWeight>(path[0], end, distance[end], path);
    }

    /// <summary>
    /// Finds the critical path using the weights carried by the graph's
    /// <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed acyclic graph to search.</param>
    /// <returns>The heaviest path, from its start vertex to its end vertex.</returns>
    /// <exception cref="InvalidOperationException">The graph has no vertices.</exception>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    public static ShortestPathResult<TVertex, TWeight> CriticalPath<TVertex, TWeight>(
        this IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.CriticalPath(edge => edge.Weight);

    private static SingleSourceShortestPaths<TVertex, TWeight> RelaxFrom<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        Func<TEdge, TWeight> weightSelector,
        bool longest)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        GraphTraversalCore.ValidateStart(graph, source);
        ArgumentNullException.ThrowIfNull(weightSelector);

        var order = graph.TopologicalSort();
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(comparer);
        Relax(graph, order, weightSelector, longest, distance, predecessor);

        return new SingleSourceShortestPaths<TVertex, TWeight>(
            source,
            distance,
            predecessor,
            new HashSet<TVertex>(graph.Vertices, comparer),
            comparer);
    }

    private static void Relax<TVertex, TEdge, TWeight>(
        IDirectedGraph<TVertex, TEdge> graph,
        IReadOnlyList<TVertex> order,
        Func<TEdge, TWeight> weightSelector,
        bool longest,
        Dictionary<TVertex, TWeight> distance,
        Dictionary<TVertex, TVertex> predecessor)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        foreach (var vertex in order)
        {
            if (!distance.TryGetValue(vertex, out var reached))
            {
                continue; // not reachable from any seed
            }

            foreach (var edge in graph.OutEdges(vertex))
            {
                var candidate = reached + weightSelector(edge);
                if (!distance.TryGetValue(edge.Target, out var known)
                    || (longest ? candidate > known : candidate < known))
                {
                    distance[edge.Target] = candidate;
                    predecessor[edge.Target] = vertex;
                }
            }
        }
    }
}
