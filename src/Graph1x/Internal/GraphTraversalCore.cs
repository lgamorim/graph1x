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
        ValidateEndpoint(graph, start, nameof(start));
    }

    /// <summary>Throws when <paramref name="vertex"/> is null or absent from <paramref name="graph"/>.</summary>
    internal static void ValidateEndpoint<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex,
        string paramName)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        if (!graph.ContainsVertex(vertex))
        {
            throw new ArgumentException($"Vertex '{vertex}' is not in the graph.", paramName);
        }
    }

    /// <summary>
    /// Enumerates the arcs leaving <paramref name="vertex"/> as (neighbor, edge)
    /// pairs: out-edges on directed graphs, incident edges with their opposite
    /// endpoint on undirected ones. Parallel edges yield one pair per instance.
    /// </summary>
    internal static IEnumerable<(TVertex Neighbor, TEdge Edge)> OutgoingArcs<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        if (graph is IDirectedGraph<TVertex, TEdge> directed)
        {
            foreach (var edge in directed.OutEdges(vertex))
            {
                yield return (edge.Target, edge);
            }
        }
        else
        {
            foreach (var edge in graph.AdjacentEdges(vertex))
            {
                yield return (OtherEndpoint(graph, edge, vertex), edge);
            }
        }
    }

    /// <summary>Rebuilds the path source → target from a predecessor map.</summary>
    internal static List<TVertex> BuildPath<TVertex>(
        TVertex source,
        TVertex target,
        Dictionary<TVertex, TVertex> predecessor,
        IEqualityComparer<TVertex> comparer)
        where TVertex : notnull
    {
        var path = new List<TVertex> { target };
        var current = target;
        while (!comparer.Equals(current, source))
        {
            current = predecessor[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
