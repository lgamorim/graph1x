using System.Numerics;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// The result of a single-source shortest-path computation: distances and a
/// predecessor tree from one source to every reachable vertex, queryable per
/// target without re-running the algorithm. The result is a snapshot of the
/// graph at computation time.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class SingleSourceShortestPaths<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly Dictionary<TVertex, TWeight> _distance;
    private readonly Dictionary<TVertex, TVertex> _predecessor;
    private readonly HashSet<TVertex> _vertices;
    private readonly IEqualityComparer<TVertex> _comparer;

    internal SingleSourceShortestPaths(
        TVertex source,
        Dictionary<TVertex, TWeight> distance,
        Dictionary<TVertex, TVertex> predecessor,
        HashSet<TVertex> vertices,
        IEqualityComparer<TVertex> comparer)
    {
        Source = source;
        _distance = distance;
        _predecessor = predecessor;
        _vertices = vertices;
        _comparer = comparer;
    }

    /// <summary>Gets the vertex the computation started from.</summary>
    public TVertex Source { get; }

    /// <summary>
    /// Gets the shortest distance to every reachable vertex (unreachable
    /// vertices are absent; the source maps to zero).
    /// </summary>
    public IReadOnlyDictionary<TVertex, TWeight> Distances => _distance;

    /// <summary>Determines whether <paramref name="target"/> is reachable from the source.</summary>
    /// <param name="target">The vertex to test.</param>
    /// <returns><see langword="true"/> if a path exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="target"/> was not part of the analyzed graph.</exception>
    public bool IsReachable(TVertex target)
    {
        ValidateKnown(target);
        return _distance.ContainsKey(target);
    }

    /// <summary>Gets the shortest path from the source to <paramref name="target"/>.</summary>
    /// <param name="target">The end vertex.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="target"/> was not part of the analyzed graph.</exception>
    public ShortestPathResult<TVertex, TWeight> To(TVertex target)
    {
        ValidateKnown(target);
        return _distance.TryGetValue(target, out var total)
            ? new ShortestPathResult<TVertex, TWeight>(
                Source,
                target,
                total,
                GraphTraversalCore.BuildPath(Source, target, _predecessor, _comparer))
            : new ShortestPathResult<TVertex, TWeight>(Source, target);
    }

    private void ValidateKnown(TVertex target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!_vertices.Contains(target))
        {
            throw new ArgumentException($"Vertex '{target}' was not part of the analyzed graph.", nameof(target));
        }
    }
}
