using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Convenience entry points for minimum-spanning-forest queries. These default
/// to Kruskal; instantiate <see cref="PrimMinimumSpanningTree{TVertex, TEdge, TWeight}"/>
/// directly to use Prim.
/// </summary>
public static class GraphMinimumSpanningTreeExtensions
{
    /// <summary>
    /// Computes a minimum spanning forest using Kruskal's algorithm and
    /// <paramref name="weightSelector"/> to read edge weights.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The undirected graph to span.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The chosen edges.</returns>
    /// <exception cref="ArgumentException"><paramref name="graph"/> is directed.</exception>
    public static IReadOnlyList<TEdge> MinimumSpanningForest<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new KruskalMinimumSpanningTree<TVertex, TEdge, TWeight>(weightSelector).FindMinimumSpanningForest(graph);

    /// <summary>
    /// Computes a minimum spanning forest using Kruskal's algorithm and the
    /// weights carried by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The undirected graph to span.</param>
    /// <returns>The chosen edges.</returns>
    /// <exception cref="ArgumentException"><paramref name="graph"/> is directed.</exception>
    public static IReadOnlyList<WeightedEdge<TVertex, TWeight>> MinimumSpanningForest<TVertex, TWeight>(
        this IReadOnlyGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.MinimumSpanningForest(edge => edge.Weight);
}
