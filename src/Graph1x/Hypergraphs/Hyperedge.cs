namespace Graph1x.Hypergraphs;

/// <summary>
/// A hyperedge: an immutable, non-empty set of vertices joined in a single
/// connection. Instances are created by
/// <see cref="Hypergraph{TVertex}.AddHyperedge(IEnumerable{TVertex})"/> and act
/// as handles — two hyperedges over the same vertex set are still distinct
/// edges of the hypergraph.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public sealed class Hyperedge<TVertex>
    where TVertex : notnull
{
    private readonly HashSet<TVertex> _vertices;

    internal Hyperedge(HashSet<TVertex> vertices)
    {
        _vertices = vertices;
    }

    /// <summary>Gets the vertices joined by this hyperedge.</summary>
    public IReadOnlySet<TVertex> Vertices => _vertices;

    /// <summary>Gets the number of distinct vertices in the hyperedge.</summary>
    public int Size => _vertices.Count;

    /// <summary>Determines whether the hyperedge joins <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to look up.</param>
    /// <returns><see langword="true"/> if the vertex is part of the hyperedge.</returns>
    public bool Contains(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _vertices.Contains(vertex);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{{{string.Join(", ", _vertices)}}}";
}
