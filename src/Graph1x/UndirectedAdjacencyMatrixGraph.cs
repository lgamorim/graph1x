using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A simple undirected graph stored as a dense symmetric adjacency matrix:
/// O(1) edge lookup and removal at O(V²) memory, the right trade-off for
/// dense graphs. Endpoint order never matters.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed class UndirectedAdjacencyMatrixGraph<TVertex, TEdge> : AdjacencyMatrixGraphBase<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Initializes an empty graph using the default vertex comparer.</summary>
    public UndirectedAdjacencyMatrixGraph()
        : base(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty graph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public UndirectedAdjacencyMatrixGraph(IEqualityComparer<TVertex> vertexComparer)
        : base(vertexComparer)
    {
    }

    /// <inheritdoc/>
    public override bool IsDirected => false;
}
