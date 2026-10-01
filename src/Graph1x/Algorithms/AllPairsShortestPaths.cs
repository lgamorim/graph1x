using System.Numerics;

namespace Graph1x.Algorithms;

/// <summary>
/// The result of an all-pairs shortest-path computation (Floyd-Warshall or
/// Johnson): shortest distances and paths between every pair of vertices,
/// queryable via <see cref="Between"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class AllPairsShortestPaths<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly TVertex[] _vertices;
    private readonly Dictionary<TVertex, int> _index;
    private readonly TWeight[,] _dist;
    private readonly bool[,] _reachable;
    private readonly int[,] _predecessor;

    /// <summary>
    /// Wraps the computed matrices. <paramref name="predecessor"/> is
    /// row-local: <c>predecessor[s, v]</c> is the vertex before <c>v</c> on
    /// <c>s</c>'s shortest path to <c>v</c>, and every row must form a tree
    /// rooted at <c>s</c>. Reconstruction walks a single row, so different
    /// rows may break ties independently.
    /// </summary>
    internal AllPairsShortestPaths(
        TVertex[] vertices,
        Dictionary<TVertex, int> index,
        TWeight[,] dist,
        bool[,] reachable,
        int[,] predecessor)
    {
        _vertices = vertices;
        _index = index;
        _dist = dist;
        _reachable = reachable;
        _predecessor = predecessor;
    }

    /// <summary>Gets the shortest path from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="ArgumentException">Either vertex was not part of the analyzed graph.</exception>
    public ShortestPathResult<TVertex, TWeight> Between(TVertex source, TVertex target)
    {
        var from = IndexOf(source, nameof(source));
        var to = IndexOf(target, nameof(target));

        if (!_reachable[from, to])
        {
            return new ShortestPathResult<TVertex, TWeight>(source, target);
        }

        var path = new List<TVertex> { _vertices[to] };
        var current = to;
        while (current != from)
        {
            current = _predecessor[from, current];
            path.Add(_vertices[current]);
        }

        path.Reverse();
        return new ShortestPathResult<TVertex, TWeight>(source, target, _dist[from, to], path);
    }

    private int IndexOf(TVertex vertex, string paramName)
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        return _index.TryGetValue(vertex, out var position)
            ? position
            : throw new ArgumentException($"Vertex '{vertex}' was not part of the analyzed graph.", paramName);
    }
}
