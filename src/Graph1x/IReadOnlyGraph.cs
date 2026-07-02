using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// Read-only view over a graph: vertex/edge enumeration and structural queries.
/// All graph implementations, directed or undirected, sparse or dense, expose
/// this contract.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public interface IReadOnlyGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Gets the number of vertices in the graph.</summary>
    int VertexCount { get; }

    /// <summary>Gets the number of edges in the graph.</summary>
    int EdgeCount { get; }

    /// <summary>Gets a value indicating whether edges are directed.</summary>
    bool IsDirected { get; }

    /// <summary>Gets a value indicating whether the graph admits parallel edges between the same endpoints.</summary>
    bool AllowsParallelEdges { get; }

    /// <summary>Gets the comparer used to identify vertices.</summary>
    IEqualityComparer<TVertex> VertexComparer { get; }

    /// <summary>Gets the vertices of the graph.</summary>
    IEnumerable<TVertex> Vertices { get; }

    /// <summary>Gets the edges of the graph.</summary>
    IEnumerable<TEdge> Edges { get; }

    /// <summary>Determines whether the graph contains <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to look up.</param>
    /// <returns><see langword="true"/> if the vertex is present.</returns>
    bool ContainsVertex(TVertex vertex);

    /// <summary>
    /// Determines whether the graph contains an edge between <paramref name="source"/>
    /// and <paramref name="target"/>. On undirected graphs the endpoint order is irrelevant.
    /// </summary>
    /// <param name="source">The first endpoint.</param>
    /// <param name="target">The second endpoint.</param>
    /// <returns><see langword="true"/> if such an edge is present.</returns>
    bool ContainsEdge(TVertex source, TVertex target);

    /// <summary>
    /// Gets the degree of <paramref name="vertex"/>. Self-loops count twice on
    /// undirected graphs; on directed graphs the degree is in-degree plus out-degree.
    /// </summary>
    /// <param name="vertex">The vertex to measure.</param>
    /// <returns>The number of edge endpoints incident to the vertex.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    int Degree(TVertex vertex);

    /// <summary>
    /// Gets the edges incident to <paramref name="vertex"/>. On directed graphs
    /// this includes both incoming and outgoing edges.
    /// </summary>
    /// <param name="vertex">The vertex whose incident edges to enumerate.</param>
    /// <returns>The incident edges.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the graph.</exception>
    IEnumerable<TEdge> AdjacentEdges(TVertex vertex);
}
