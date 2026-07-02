namespace Graph1x.Edges;

/// <summary>
/// An edge connecting two vertices. Edge values are ordered pairs; undirected
/// semantics (where a-b and b-a denote the same connection) are applied by the
/// graph that stores the edge, not by the edge itself.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public interface IEdge<TVertex>
    where TVertex : notnull
{
    /// <summary>Gets the vertex the edge starts at.</summary>
    TVertex Source { get; }

    /// <summary>Gets the vertex the edge ends at.</summary>
    TVertex Target { get; }
}
