using System.Numerics;

namespace Graph1x.Algorithms;

/// <summary>
/// The outcome of a shortest-path query: whether the target is reachable, the
/// total distance, and the vertex path from source to target.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class ShortestPathResult<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly TWeight _distance;

    /// <summary>Initializes a reachable result.</summary>
    /// <param name="source">The path's start vertex.</param>
    /// <param name="target">The path's end vertex.</param>
    /// <param name="distance">The total path weight.</param>
    /// <param name="path">The vertices from <paramref name="source"/> to <paramref name="target"/> inclusive.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ShortestPathResult(TVertex source, TVertex target, TWeight distance, IReadOnlyList<TVertex> path)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(path);
        Source = source;
        Target = target;
        IsReachable = true;
        _distance = distance;
        Path = path;
    }

    /// <summary>Initializes an unreachable result.</summary>
    /// <param name="source">The path's start vertex.</param>
    /// <param name="target">The unreachable end vertex.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ShortestPathResult(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Source = source;
        Target = target;
        IsReachable = false;
        _distance = TWeight.Zero;
        Path = [];
    }

    /// <summary>Gets the vertex the query started from.</summary>
    public TVertex Source { get; }

    /// <summary>Gets the vertex the query aimed for.</summary>
    public TVertex Target { get; }

    /// <summary>Gets a value indicating whether a path exists.</summary>
    public bool IsReachable { get; }

    /// <summary>Gets the total weight of the shortest path.</summary>
    /// <exception cref="InvalidOperationException">The target is not reachable.</exception>
    public TWeight Distance => IsReachable
        ? _distance
        : throw new InvalidOperationException($"'{Target}' is not reachable from '{Source}'; check IsReachable before reading Distance.");

    /// <summary>
    /// Gets the vertices of the shortest path from <see cref="Source"/> to
    /// <see cref="Target"/> inclusive, or an empty list when unreachable.
    /// </summary>
    public IReadOnlyList<TVertex> Path { get; }
}
