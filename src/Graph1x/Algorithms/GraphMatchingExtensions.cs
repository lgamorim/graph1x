using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Convenience entry points for matching queries.
/// </summary>
public static class GraphMatchingExtensions
{
    /// <summary>
    /// Computes a maximum-cardinality matching of an undirected bipartite
    /// graph using Hopcroft-Karp.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The undirected bipartite graph.</param>
    /// <returns>The matched edges; no two share a vertex, and no larger matching exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="graph"/> is directed or not bipartite.</exception>
    public static IReadOnlyList<TEdge> MaximumBipartiteMatching<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => new HopcroftKarpMatching<TVertex, TEdge>().FindMaximumMatching(graph);
}
