using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// A minimum-spanning-tree strategy for undirected graphs. On disconnected
/// graphs implementations return a minimum spanning forest (one tree per
/// connected component). Kruskal and Prim are interchangeable behind this
/// interface.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public interface IMinimumSpanningTreeAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    /// <summary>Computes a minimum spanning forest of the graph.</summary>
    /// <param name="graph">The undirected graph to span.</param>
    /// <returns>The chosen edges: |V| - #components of them, total weight minimal.</returns>
    /// <exception cref="ArgumentException"><paramref name="graph"/> is directed.</exception>
    IReadOnlyList<TEdge> FindMinimumSpanningForest(IReadOnlyGraph<TVertex, TEdge> graph);
}
