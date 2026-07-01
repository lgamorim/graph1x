using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A graph whose edges are directed, adding in/out refinements of the
/// undirected structural queries.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public interface IDirectedGraph<TVertex, TEdge> : IReadOnlyGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Gets the number of edges leaving <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to measure.</param>
    /// <returns>The out-degree.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    int OutDegree(TVertex vertex);

    /// <summary>Gets the number of edges entering <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to measure.</param>
    /// <returns>The in-degree.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    int InDegree(TVertex vertex);

    /// <summary>Gets the edges leaving <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex whose outgoing edges to enumerate.</param>
    /// <returns>The outgoing edges.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    IEnumerable<TEdge> OutEdges(TVertex vertex);

    /// <summary>Gets the edges entering <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex whose incoming edges to enumerate.</param>
    /// <returns>The incoming edges.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    IEnumerable<TEdge> InEdges(TVertex vertex);
}
