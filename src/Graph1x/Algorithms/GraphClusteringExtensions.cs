using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Clustering coefficients: how close each vertex's neighborhood is to a
/// clique. Edge direction is ignored, self-loops never count, and on
/// multigraphs neighbors are counted once regardless of parallel edges.
/// </summary>
public static class GraphClusteringExtensions
{
    /// <summary>
    /// Gets the local clustering coefficient of <paramref name="vertex"/>:
    /// the fraction of its distinct-neighbor pairs that are themselves
    /// connected. Vertices with fewer than two neighbors score 0.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="vertex">The vertex to measure.</param>
    /// <returns>The local clustering coefficient in [0, 1].</returns>
    /// <exception cref="ArgumentException"><paramref name="vertex"/> is not in the graph.</exception>
    public static double LocalClusteringCoefficient<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, vertex, nameof(vertex));

        var (links, pairs) = NeighborhoodLinks(graph, vertex);
        return pairs > 0 ? (double)links / pairs : 0.0;
    }

    /// <summary>Gets the local clustering coefficient of every vertex.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>The local clustering coefficient per vertex.</returns>
    public static IReadOnlyDictionary<TVertex, double> ClusteringCoefficients<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var coefficients = new Dictionary<TVertex, double>(graph.VertexCount, graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            var (links, pairs) = NeighborhoodLinks(graph, vertex);
            coefficients[vertex] = pairs > 0 ? (double)links / pairs : 0.0;
        }

        return coefficients;
    }

    /// <summary>
    /// Gets the average of the local clustering coefficients over all
    /// vertices (0 for the empty graph).
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>The average clustering coefficient in [0, 1].</returns>
    public static double AverageClusteringCoefficient<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.VertexCount == 0)
        {
            return 0.0;
        }

        var total = 0.0;
        foreach (var vertex in graph.Vertices)
        {
            var (links, pairs) = NeighborhoodLinks(graph, vertex);
            total += pairs > 0 ? (double)links / pairs : 0.0;
        }

        return total / graph.VertexCount;
    }

    /// <summary>
    /// Gets the global clustering coefficient (transitivity): three times the
    /// triangle count over the number of connected triples. Graphs without
    /// connected triples score 0.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>The global clustering coefficient in [0, 1].</returns>
    public static double GlobalClusteringCoefficient<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Per-vertex neighborhood links sum to 3× the triangle count (each
        // triangle is seen once from each corner); pair counts sum to the
        // connected-triple count.
        long closed = 0;
        long triples = 0;
        foreach (var vertex in graph.Vertices)
        {
            var (links, pairs) = NeighborhoodLinks(graph, vertex);
            closed += links;
            triples += pairs;
        }

        return triples > 0 ? (double)closed / triples : 0.0;
    }

    /// <summary>
    /// Counts the edges among a vertex's distinct neighbors (direction
    /// ignored, self excluded) and the number of neighbor pairs.
    /// </summary>
    private static (long Links, long Pairs) NeighborhoodLinks<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var neighbors = new HashSet<TVertex>(comparer);
        foreach (var edge in graph.AdjacentEdges(vertex))
        {
            var source = edge.Source;
            var target = edge.Target;
            var other = comparer.Equals(source, vertex) ? target : source;
            if (!comparer.Equals(other, vertex))
            {
                neighbors.Add(other);
            }
        }

        if (neighbors.Count < 2)
        {
            return (0, 0);
        }

        var list = new List<TVertex>(neighbors);
        long links = 0;
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                if (graph.ContainsEdge(list[i], list[j]) || graph.ContainsEdge(list[j], list[i]))
                {
                    links++;
                }
            }
        }

        long count = neighbors.Count;
        return (links, count * (count - 1) / 2);
    }
}
