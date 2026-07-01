using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Kruskal's minimum-spanning-tree algorithm: edges are considered in
/// ascending weight order and accepted when they join two different
/// components, tracked with a union-find structure. Naturally yields a
/// spanning forest on disconnected graphs; negative weights are fine.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class KruskalMinimumSpanningTree<TVertex, TEdge, TWeight> : IMinimumSpanningTreeAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public KruskalMinimumSpanningTree(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
    }

    /// <inheritdoc/>
    public IReadOnlyList<TEdge> FindMinimumSpanningForest(IReadOnlyGraph<TVertex, TEdge> graph)
    {
        MinimumSpanningTreeGuard.RequireUndirected(graph);

        var components = new DisjointSet<TVertex>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            components.MakeSet(vertex);
        }

        var forest = new List<TEdge>();
        foreach (var edge in graph.Edges.OrderBy(_weightSelector))
        {
            if (components.Union(edge.Source, edge.Target))
            {
                forest.Add(edge);
            }
        }

        return forest;
    }
}

/// <summary>Shared argument validation for MST strategies.</summary>
internal static class MinimumSpanningTreeGuard
{
    internal static void RequireUndirected<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsDirected)
        {
            throw new ArgumentException(
                "Minimum spanning trees are defined for undirected graphs.", nameof(graph));
        }
    }
}
