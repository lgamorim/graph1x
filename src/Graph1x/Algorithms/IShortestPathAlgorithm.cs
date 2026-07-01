using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// A single-pair shortest-path strategy. Implementations (Dijkstra,
/// Bellman-Ford, A*) are interchangeable behind this interface; pick by the
/// graph's weight profile (non-negative, negative edges, heuristic available).
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public interface IShortestPathAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    /// <summary>Finds the shortest path from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="ArgumentException">Either endpoint is not in the graph.</exception>
    ShortestPathResult<TVertex, TWeight> FindPath(IReadOnlyGraph<TVertex, TEdge> graph, TVertex source, TVertex target);
}
