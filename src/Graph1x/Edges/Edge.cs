namespace Graph1x.Edges;

/// <summary>
/// An unweighted edge between two vertices, with value equality over the
/// (source, target) pair.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public readonly record struct Edge<TVertex> : IEdge<TVertex>
    where TVertex : notnull
{
    /// <summary>Initializes a new edge from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="source">The vertex the edge starts at.</param>
    /// <param name="target">The vertex the edge ends at.</param>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null"/>.</exception>
    public Edge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Source = source;
        Target = target;
    }

    /// <inheritdoc/>
    public TVertex Source { get; }

    /// <inheritdoc/>
    public TVertex Target { get; }

    /// <summary>Deconstructs the edge into its endpoints.</summary>
    /// <param name="source">The vertex the edge starts at.</param>
    /// <param name="target">The vertex the edge ends at.</param>
    public void Deconstruct(out TVertex source, out TVertex target)
    {
        source = Source;
        target = Target;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Source} -> {Target}";
}
