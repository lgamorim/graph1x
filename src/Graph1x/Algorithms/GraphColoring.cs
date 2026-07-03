namespace Graph1x.Algorithms;

/// <summary>
/// A proper vertex coloring: adjacent vertices always receive different
/// colors. Colors are contiguous integers starting at zero, and
/// <see cref="ColorCount"/> is an upper bound on the chromatic number.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public sealed class GraphColoring<TVertex>
    where TVertex : notnull
{
    private readonly Dictionary<TVertex, int> _colors;

    internal GraphColoring(Dictionary<TVertex, int> colors, int colorCount)
    {
        _colors = colors;
        ColorCount = colorCount;
    }

    /// <summary>Gets the color assigned to every vertex.</summary>
    public IReadOnlyDictionary<TVertex, int> Colors => _colors;

    /// <summary>Gets the number of distinct colors used (an upper bound on the chromatic number).</summary>
    public int ColorCount { get; }

    /// <summary>Gets the color assigned to <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to look up.</param>
    /// <returns>The zero-based color.</returns>
    /// <exception cref="ArgumentException"><paramref name="vertex"/> was not part of the colored graph.</exception>
    public int ColorOf(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _colors.TryGetValue(vertex, out var color)
            ? color
            : throw new ArgumentException($"Vertex '{vertex}' was not part of the colored graph.", nameof(vertex));
    }
}
