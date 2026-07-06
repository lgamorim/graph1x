using Graph1x.Edges;

namespace Graph1x.Internal;

/// <summary>
/// Shared "make a graph like this one" dispatch: picks the adjacency-list
/// family matching a source graph's direction and parallel-edge policy.
/// </summary>
internal static class GraphCloneCore
{
    /// <summary>Creates an empty mutable graph matching the source's direction, parallel-edge policy, and comparer.</summary>
    internal static IMutableGraph<TVertex, TEdge> CreateEmptyLike<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => (graph.IsDirected, graph.AllowsParallelEdges) switch
        {
            (true, true) => new DirectedMultigraph<TVertex, TEdge>(graph.VertexComparer),
            (true, false) => new DirectedGraph<TVertex, TEdge>(graph.VertexComparer),
            (false, true) => new UndirectedMultigraph<TVertex, TEdge>(graph.VertexComparer),
            (false, false) => new UndirectedGraph<TVertex, TEdge>(graph.VertexComparer),
        };

    /// <summary>Deep-copies the graph into a fresh mutable graph of the matching family.</summary>
    internal static IMutableGraph<TVertex, TEdge> Copy<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var copy = CreateEmptyLike(graph);
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
