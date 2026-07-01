using Graph1x.Edges;

namespace Graph1x.Internal;

/// <summary>Shared traversal primitives.</summary>
internal static class GraphTraversalCore
{
    /// <summary>
    /// Enumerates the vertices reachable from <paramref name="vertex"/> in one
    /// step: out-edge targets on directed graphs, opposite endpoints on
    /// undirected ones. Parallel edges yield their successor once per instance.
    /// </summary>
    internal static IEnumerable<TVertex> Successors<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph, TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        if (graph is IDirectedGraph<TVertex, TEdge> directed)
        {
            foreach (var edge in directed.OutEdges(vertex))
            {
                yield return edge.Target;
            }
        }
        else
        {
            foreach (var edge in graph.AdjacentEdges(vertex))
            {
                yield return OtherEndpoint(graph, edge, vertex);
            }
        }
    }

    /// <summary>Gets the endpoint of <paramref name="edge"/> that is not <paramref name="vertex"/> (or the vertex itself for self-loops).</summary>
    internal static TVertex OtherEndpoint<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph, TEdge edge, TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.VertexComparer.Equals(edge.Source, vertex) ? edge.Target : edge.Source;

    /// <summary>Validates the standard (graph, start) argument pair for traversal entry points.</summary>
    internal static void ValidateStart<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph, TVertex start)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(start);
        if (!graph.ContainsVertex(start))
        {
            throw new ArgumentException($"Start vertex '{start}' is not in the graph.", nameof(start));
        }
    }
}
