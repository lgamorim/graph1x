using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A simple directed graph stored as a dense adjacency matrix: O(1) edge
/// lookup and removal at O(V²) memory, the right trade-off for dense graphs.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed class DirectedAdjacencyMatrixGraph<TVertex, TEdge>
    : AdjacencyMatrixGraphBase<TVertex, TEdge>, IDirectedGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Initializes an empty graph using the default vertex comparer.</summary>
    public DirectedAdjacencyMatrixGraph()
        : base(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty graph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public DirectedAdjacencyMatrixGraph(IEqualityComparer<TVertex> vertexComparer)
        : base(vertexComparer)
    {
    }

    /// <inheritdoc/>
    public override bool IsDirected => true;

    /// <inheritdoc/>
    public int OutDegree(TVertex vertex) => CountRow(IndexOf(vertex));

    /// <inheritdoc/>
    public int InDegree(TVertex vertex) => CountColumn(IndexOf(vertex));

    /// <inheritdoc/>
    public IEnumerable<TEdge> OutEdges(TVertex vertex) => EdgesInRow(IndexOf(vertex));

    /// <inheritdoc/>
    public IEnumerable<TEdge> InEdges(TVertex vertex) => EdgesInColumn(IndexOf(vertex));
}
