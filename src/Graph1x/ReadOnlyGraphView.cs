using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A live read-only wrapper: delegates every query to the underlying graph
/// but does not implement <see cref="IMutableGraph{TVertex, TEdge}"/>, so the
/// wrapped graph cannot be mutated through it.
/// </summary>
internal class ReadOnlyGraphView<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> inner) : IReadOnlyGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private readonly IReadOnlyGraph<TVertex, TEdge> _inner = inner;

    public int VertexCount => _inner.VertexCount;

    public int EdgeCount => _inner.EdgeCount;

    public bool IsDirected => _inner.IsDirected;

    public bool AllowsParallelEdges => _inner.AllowsParallelEdges;

    public IEqualityComparer<TVertex> VertexComparer => _inner.VertexComparer;

    public IEnumerable<TVertex> Vertices => _inner.Vertices;

    public IEnumerable<TEdge> Edges => _inner.Edges;

    public bool ContainsVertex(TVertex vertex) => _inner.ContainsVertex(vertex);

    public bool ContainsEdge(TVertex source, TVertex target) => _inner.ContainsEdge(source, target);

    public int Degree(TVertex vertex) => _inner.Degree(vertex);

    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex) => _inner.AdjacentEdges(vertex);
}

/// <summary>
/// The directed refinement of <see cref="ReadOnlyGraphView{TVertex, TEdge}"/>:
/// implements <see cref="IDirectedGraph{TVertex, TEdge}"/> so algorithms keep
/// dispatching directed semantics through the wrapper.
/// </summary>
internal sealed class ReadOnlyDirectedGraphView<TVertex, TEdge>(IDirectedGraph<TVertex, TEdge> inner)
    : ReadOnlyGraphView<TVertex, TEdge>(inner), IDirectedGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private readonly IDirectedGraph<TVertex, TEdge> _inner = inner;

    public int OutDegree(TVertex vertex) => _inner.OutDegree(vertex);

    public int InDegree(TVertex vertex) => _inner.InDegree(vertex);

    public IEnumerable<TEdge> OutEdges(TVertex vertex) => _inner.OutEdges(vertex);

    public IEnumerable<TEdge> InEdges(TVertex vertex) => _inner.InEdges(vertex);
}
