using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// The Bellman-Ford shortest-path algorithm. Supports negative edge weights;
/// throws <see cref="NegativeCycleException"/> when a negative cycle is
/// reachable from the source. On undirected graphs each edge acts as two
/// opposite arcs, so any negative undirected edge is itself a negative cycle.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class BellmanFordShortestPath<TVertex, TEdge, TWeight> : IShortestPathAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public BellmanFordShortestPath(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
    }

    /// <inheritdoc/>
    /// <exception cref="NegativeCycleException">A negative cycle is reachable from <paramref name="source"/>.</exception>
    public ShortestPathResult<TVertex, TWeight> FindPath(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex target)
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));
        GraphTraversalCore.ValidateEndpoint(graph, target, nameof(target));

        var (distance, predecessor) = RelaxAll(graph, source);

        return distance.TryGetValue(target, out var total)
            ? new ShortestPathResult<TVertex, TWeight>(
                source,
                target,
                total,
                GraphTraversalCore.BuildPath(source, target, predecessor, graph.VertexComparer))
            : new ShortestPathResult<TVertex, TWeight>(source, target);
    }

    /// <summary>
    /// Computes shortest paths from <paramref name="source"/> to every
    /// reachable vertex in one run, for querying many targets without
    /// re-running the algorithm. Negative edge weights are supported.
    /// </summary>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <returns>A queryable single-source result.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    /// <exception cref="NegativeCycleException">A negative cycle is reachable from <paramref name="source"/>.</exception>
    public SingleSourceShortestPaths<TVertex, TWeight> FindPathsFrom(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source)
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));

        var (distance, predecessor) = RelaxAll(graph, source);
        return new SingleSourceShortestPaths<TVertex, TWeight>(
            source,
            distance,
            predecessor,
            new HashSet<TVertex>(graph.Vertices, graph.VertexComparer),
            graph.VertexComparer);
    }

    private (Dictionary<TVertex, TWeight> Distance, Dictionary<TVertex, TVertex> Predecessor) RelaxAll(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source)
    {
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(comparer);

        for (var pass = 1; pass < graph.VertexCount; pass++)
        {
            var relaxed = false;
            foreach (var (from, to, weight) in Arcs(graph))
            {
                if (Relax(from, to, weight, distance, predecessor))
                {
                    relaxed = true;
                }
            }

            if (!relaxed)
            {
                break;
            }
        }

        foreach (var (from, to, weight) in Arcs(graph))
        {
            if (Relax(from, to, weight, distance, predecessor))
            {
                throw new NegativeCycleException(
                    $"A negative-weight cycle reachable from '{source}' was detected; shortest distances are undefined.");
            }
        }

        return (distance, predecessor);
    }

    private static bool Relax(
        TVertex from,
        TVertex to,
        TWeight weight,
        Dictionary<TVertex, TWeight> distance,
        Dictionary<TVertex, TVertex> predecessor)
    {
        if (!distance.TryGetValue(from, out var fromDistance))
        {
            return false;
        }

        var candidate = fromDistance + weight;
        if (distance.TryGetValue(to, out var known) && known <= candidate)
        {
            return false;
        }

        distance[to] = candidate;
        predecessor[to] = from;
        return true;
    }

    private IEnumerable<(TVertex From, TVertex To, TWeight Weight)> Arcs(IReadOnlyGraph<TVertex, TEdge> graph)
    {
        foreach (var edge in graph.Edges)
        {
            var weight = _weightSelector(edge);
            yield return (edge.Source, edge.Target, weight);
            if (!graph.IsDirected && !graph.VertexComparer.Equals(edge.Source, edge.Target))
            {
                yield return (edge.Target, edge.Source, weight);
            }
        }
    }
}
