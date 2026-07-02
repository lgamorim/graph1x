using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x;

/// <summary>
/// An undirected multigraph: parallel edges between the same endpoints and
/// self-loops are all allowed, and endpoint order never matters. Backed by
/// per-vertex incidence lists.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public class UndirectedMultigraph<TVertex, TEdge> : IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    // Each edge instance is listed under both endpoints (once for self-loops).
    private readonly Dictionary<TVertex, List<TEdge>> _incidence;

    /// <summary>Initializes an empty multigraph using the default vertex comparer.</summary>
    public UndirectedMultigraph()
        : this(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty multigraph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public UndirectedMultigraph(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _incidence = new Dictionary<TVertex, List<TEdge>>(vertexComparer);
    }

    /// <inheritdoc/>
    public int VertexCount => _incidence.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public bool IsDirected => false;

    /// <inheritdoc/>
    public bool AllowsParallelEdges => true;

    /// <inheritdoc/>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <inheritdoc/>
    public IEnumerable<TVertex> Vertices => _incidence.Keys;

    /// <inheritdoc/>
    public IEnumerable<TEdge> Edges
    {
        get
        {
            foreach (var (vertex, incidence) in _incidence)
            {
                foreach (var edge in incidence)
                {
                    // Every edge is listed under both endpoints; yield it only
                    // from its source endpoint so each instance comes out once.
                    if (VertexComparer.Equals(vertex, edge.Source))
                    {
                        yield return edge;
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool AddVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (_incidence.ContainsKey(vertex))
        {
            return false;
        }

        _incidence.Add(vertex, []);
        return true;
    }

    /// <inheritdoc/>
    public bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        _incidence[edge.Source].Add(edge);
        if (!VertexComparer.Equals(edge.Source, edge.Target))
        {
            _incidence[edge.Target].Add(edge);
        }

        EdgeCount++;
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!_incidence.TryGetValue(vertex, out var incidence))
        {
            return false;
        }

        foreach (var edge in incidence)
        {
            var other = OtherEndpoint(edge, vertex);
            if (!VertexComparer.Equals(other, vertex))
            {
                _incidence[other].Remove(edge);
            }
        }

        EdgeCount -= incidence.Count;
        _incidence.Remove(vertex);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_incidence.TryGetValue(edge.Source, out var incidence) || !incidence.Remove(edge))
        {
            return false;
        }

        if (!VertexComparer.Equals(edge.Source, edge.Target))
        {
            _incidence[edge.Target].Remove(edge);
        }

        EdgeCount--;
        return true;
    }

    /// <summary>Removes one edge between <paramref name="source"/> and <paramref name="target"/>, whatever its payload or orientation.</summary>
    /// <param name="source">One endpoint.</param>
    /// <param name="target">The other endpoint.</param>
    /// <returns><see langword="true"/> if an edge was removed.</returns>
    public bool RemoveEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!_incidence.TryGetValue(source, out var incidence))
        {
            return false;
        }

        foreach (var edge in incidence)
        {
            if (VertexComparer.Equals(OtherEndpoint(edge, source), target))
            {
                return RemoveEdge(edge);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _incidence.Clear();
        EdgeCount = 0;
    }

    /// <inheritdoc/>
    public bool ContainsVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _incidence.ContainsKey(vertex);
    }

    /// <inheritdoc/>
    public bool ContainsEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _incidence.TryGetValue(source, out var incidence)
            && incidence.Any(edge => VertexComparer.Equals(OtherEndpoint(edge, source), target));
    }

    /// <summary>Gets every parallel edge between <paramref name="source"/> and <paramref name="target"/>, in either orientation.</summary>
    /// <param name="source">One endpoint.</param>
    /// <param name="target">The other endpoint.</param>
    /// <returns>The matching edges, possibly empty.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    public IEnumerable<TEdge> GetEdges(TVertex source, TVertex target)
    {
        Guard.VertexExists(_incidence, source);
        ArgumentNullException.ThrowIfNull(target);
        return _incidence[source].Where(edge => VertexComparer.Equals(OtherEndpoint(edge, source), target));
    }

    /// <inheritdoc/>
    public int Degree(TVertex vertex)
    {
        Guard.VertexExists(_incidence, vertex);
        var incidence = _incidence[vertex];

        // A self-loop contributes two endpoints to the degree but is listed once.
        return incidence.Count + incidence.Count(edge => VertexComparer.Equals(edge.Source, edge.Target));
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex)
    {
        Guard.VertexExists(_incidence, vertex);
        return _incidence[vertex];
    }

    private TVertex OtherEndpoint(TEdge edge, TVertex vertex)
        => VertexComparer.Equals(edge.Source, vertex) ? edge.Target : edge.Source;
}
