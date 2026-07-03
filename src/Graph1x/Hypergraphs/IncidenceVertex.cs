namespace Graph1x.Hypergraphs;

/// <summary>
/// Factory entry points for <see cref="IncidenceVertex{TVertex}"/> nodes
/// (kept non-generic so call sites can rely on type inference).
/// </summary>
public static class IncidenceVertex
{
    /// <summary>Creates the incidence node representing an original hypergraph vertex.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="vertex">The hypergraph vertex.</param>
    /// <returns>The vertex node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex"/> is <see langword="null"/>.</exception>
    public static IncidenceVertex<TVertex> ForVertex<TVertex>(TVertex vertex)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return new IncidenceVertex<TVertex>(vertex, -1);
    }

    /// <summary>Creates the incidence node representing a hyperedge.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="hyperedgeIndex">The hyperedge's zero-based position in the hypergraph's enumeration order.</param>
    /// <returns>The hyperedge node.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="hyperedgeIndex"/> is negative.</exception>
    public static IncidenceVertex<TVertex> ForHyperedge<TVertex>(int hyperedgeIndex)
        where TVertex : notnull
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hyperedgeIndex);
        return new IncidenceVertex<TVertex>(default, hyperedgeIndex);
    }
}

/// <summary>
/// A node of a bipartite incidence expansion: either an original hypergraph
/// vertex or a node standing in for one hyperedge. Create instances via
/// <see cref="IncidenceVertex.ForVertex{TVertex}"/> and
/// <see cref="IncidenceVertex.ForHyperedge{TVertex}"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public readonly struct IncidenceVertex<TVertex> : IEquatable<IncidenceVertex<TVertex>>
    where TVertex : notnull
{
    private readonly TVertex? _vertex;
    private readonly int _hyperedgeIndex;

    internal IncidenceVertex(TVertex? vertex, int hyperedgeIndex)
    {
        _vertex = vertex;
        _hyperedgeIndex = hyperedgeIndex;
    }

    /// <summary>Gets a value indicating whether this node stands in for a hyperedge.</summary>
    public bool IsHyperedge => _hyperedgeIndex >= 0;

    /// <summary>Gets the original hypergraph vertex.</summary>
    /// <exception cref="InvalidOperationException">This node is a hyperedge node.</exception>
    public TVertex Vertex => !IsHyperedge
        ? _vertex!
        : throw new InvalidOperationException("This incidence node stands in for a hyperedge, not a vertex.");

    /// <summary>Gets the hyperedge's zero-based position in the hypergraph's enumeration order.</summary>
    /// <exception cref="InvalidOperationException">This node is a vertex node.</exception>
    public int HyperedgeIndex => IsHyperedge
        ? _hyperedgeIndex
        : throw new InvalidOperationException("This incidence node stands in for a vertex, not a hyperedge.");

    /// <inheritdoc/>
    public bool Equals(IncidenceVertex<TVertex> other)
        => _hyperedgeIndex == other._hyperedgeIndex
            && (IsHyperedge || EqualityComparer<TVertex>.Default.Equals(_vertex!, other._vertex!));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is IncidenceVertex<TVertex> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => IsHyperedge ? _hyperedgeIndex : EqualityComparer<TVertex>.Default.GetHashCode(_vertex!);

    /// <summary>Determines whether two nodes are equal.</summary>
    public static bool operator ==(IncidenceVertex<TVertex> left, IncidenceVertex<TVertex> right)
        => left.Equals(right);

    /// <summary>Determines whether two nodes are different.</summary>
    public static bool operator !=(IncidenceVertex<TVertex> left, IncidenceVertex<TVertex> right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => IsHyperedge ? $"e{_hyperedgeIndex}" : _vertex!.ToString() ?? string.Empty;
}
