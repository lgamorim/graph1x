using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A graph that supports in-place mutation. Add/Remove operations follow the
/// .NET collection idiom: they return <see langword="false"/> for duplicates or
/// missing items instead of throwing.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public interface IMutableGraph<TVertex, TEdge> : IReadOnlyGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Adds <paramref name="vertex"/> to the graph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns><see langword="true"/> if added; <see langword="false"/> if it was already present.</returns>
    bool AddVertex(TVertex vertex);

    /// <summary>
    /// Adds <paramref name="edge"/> to the graph, adding missing endpoint
    /// vertices automatically.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <returns>
    /// <see langword="true"/> if added; <see langword="false"/> if the graph
    /// forbids the edge (e.g. a parallel edge on a simple graph).
    /// </returns>
    bool AddEdge(TEdge edge);

    /// <summary>Removes <paramref name="vertex"/> and every edge incident to it.</summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if it was not present.</returns>
    bool RemoveVertex(TVertex vertex);

    /// <summary>Removes <paramref name="edge"/> from the graph. Endpoint vertices stay.</summary>
    /// <param name="edge">The edge to remove.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if it was not present.</returns>
    bool RemoveEdge(TEdge edge);

    /// <summary>Removes every vertex and edge from the graph.</summary>
    void Clear();
}
