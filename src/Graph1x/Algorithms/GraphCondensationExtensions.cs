using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Condensation: collapse each strongly connected component to a single
/// vertex, producing a DAG on which DAG-only tools (topological sort,
/// transitive reduction) become applicable to any directed graph.
/// </summary>
public static class GraphCondensationExtensions
{
    /// <summary>Builds the condensation of the graph.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to condense.</param>
    /// <returns>
    /// The condensation DAG plus component lookups. Inter-component edges
    /// dedupe; intra-component edges (including self-loops) disappear.
    /// </returns>
    public static CondensationResult<TVertex> Condense<TVertex, TEdge>(this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var components = graph.StronglyConnectedComponents();
        var componentOf = new Dictionary<TVertex, int>(graph.VertexComparer);
        for (var index = 0; index < components.Count; index++)
        {
            foreach (var vertex in components[index])
            {
                componentOf[vertex] = index;
            }
        }

        var condensed = new DirectedGraph<int, Edge<int>>();
        for (var index = 0; index < components.Count; index++)
        {
            condensed.AddVertex(index);
        }

        foreach (var edge in graph.Edges)
        {
            var source = componentOf[edge.Source];
            var target = componentOf[edge.Target];
            if (source != target)
            {
                condensed.AddEdge(new Edge<int>(source, target));
            }
        }

        return new CondensationResult<TVertex>(condensed, components, componentOf);
    }
}
