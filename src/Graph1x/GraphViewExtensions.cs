using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// Read-only views and immutable snapshots over any graph. A view is live
/// (later mutations of the underlying graph show through); a frozen graph is
/// a deep copy that never changes and is safe for concurrent readers.
/// </summary>
public static class GraphViewExtensions
{
    /// <summary>
    /// Wraps the graph in a live read-only view that cannot be cast back to
    /// <see cref="IMutableGraph{TVertex, TEdge}"/>. Directed graphs yield a
    /// view that still implements <see cref="IDirectedGraph{TVertex, TEdge}"/>;
    /// calling this on an existing view returns the same instance.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to wrap.</param>
    /// <returns>A read-only live view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IReadOnlyGraph<TVertex, TEdge> AsReadOnly<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph switch
        {
            ReadOnlyGraphView<TVertex, TEdge> view => view,
            IDirectedGraph<TVertex, TEdge> directed => new ReadOnlyDirectedGraphView<TVertex, TEdge>(directed),
            _ => new ReadOnlyGraphView<TVertex, TEdge>(graph),
        };
    }

    /// <summary>
    /// Wraps a directed graph in a live read-only view, keeping the
    /// <see cref="IDirectedGraph{TVertex, TEdge}"/> static type.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to wrap.</param>
    /// <returns>A read-only live view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IDirectedGraph<TVertex, TEdge> AsReadOnly<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph as ReadOnlyDirectedGraphView<TVertex, TEdge>
            ?? new ReadOnlyDirectedGraphView<TVertex, TEdge>(graph);
    }

    /// <summary>
    /// Takes an immutable snapshot of the graph: a deep copy (matching the
    /// source's direction and parallel-edge policy) wrapped read-only. The
    /// snapshot never changes and is safe for concurrent readers.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to snapshot.</param>
    /// <returns>An immutable copy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IReadOnlyGraph<TVertex, TEdge> ToFrozen<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return Copy(graph).AsReadOnly();
    }

    /// <summary>
    /// Takes an immutable snapshot of a directed graph, keeping the
    /// <see cref="IDirectedGraph{TVertex, TEdge}"/> static type.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to snapshot.</param>
    /// <returns>An immutable copy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IDirectedGraph<TVertex, TEdge> ToFrozen<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return (IDirectedGraph<TVertex, TEdge>)Copy(graph).AsReadOnly();
    }

    private static IMutableGraph<TVertex, TEdge> Copy<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        IMutableGraph<TVertex, TEdge> copy = (graph.IsDirected, graph.AllowsParallelEdges) switch
        {
            (true, true) => new DirectedMultigraph<TVertex, TEdge>(graph.VertexComparer),
            (true, false) => new DirectedGraph<TVertex, TEdge>(graph.VertexComparer),
            (false, true) => new UndirectedMultigraph<TVertex, TEdge>(graph.VertexComparer),
            (false, false) => new UndirectedGraph<TVertex, TEdge>(graph.VertexComparer),
        };

        foreach (var vertex in graph.Vertices)
        {
            copy.AddVertex(vertex);
        }

        foreach (var edge in graph.Edges)
        {
            copy.AddEdge(edge);
        }

        return copy;
    }
}
