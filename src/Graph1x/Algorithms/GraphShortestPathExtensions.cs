using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Convenience entry points for shortest-path queries. These default to
/// Dijkstra; instantiate <see cref="BellmanFordShortestPath{TVertex, TEdge, TWeight}"/>,
/// <see cref="AStarShortestPath{TVertex, TEdge, TWeight}"/>, or
/// <see cref="FloydWarshallAllShortestPaths{TVertex, TEdge, TWeight}"/> directly
/// when negative weights, a heuristic, or all-pairs results are needed.
/// </summary>
public static class GraphShortestPathExtensions
{
    /// <summary>
    /// Finds the shortest path from <paramref name="source"/> to
    /// <paramref name="target"/> using Dijkstra's algorithm and
    /// <paramref name="weightSelector"/> to read edge weights.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static ShortestPathResult<TVertex, TWeight> ShortestPath<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex target,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new DijkstraShortestPath<TVertex, TEdge, TWeight>(weightSelector).FindPath(graph, source, target);

    /// <summary>
    /// Finds the shortest path from <paramref name="source"/> to
    /// <paramref name="target"/> using Dijkstra's algorithm and the weights
    /// carried by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static ShortestPathResult<TVertex, TWeight> ShortestPath<TVertex, TWeight>(
        this IReadOnlyGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source,
        TVertex target)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.ShortestPath(source, target, edge => edge.Weight);

    /// <summary>
    /// Computes shortest paths from <paramref name="source"/> to every
    /// reachable vertex using Dijkstra's algorithm and
    /// <paramref name="weightSelector"/> to read edge weights.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>A queryable single-source result.</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> ShortestPathsFrom<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new DijkstraShortestPath<TVertex, TEdge, TWeight>(weightSelector).FindPathsFrom(graph, source);

    /// <summary>
    /// Computes shortest paths from <paramref name="source"/> to every
    /// reachable vertex using Dijkstra's algorithm and the weights carried by
    /// the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <returns>A queryable single-source result.</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static SingleSourceShortestPaths<TVertex, TWeight> ShortestPathsFrom<TVertex, TWeight>(
        this IReadOnlyGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.ShortestPathsFrom(source, edge => edge.Weight);
}
