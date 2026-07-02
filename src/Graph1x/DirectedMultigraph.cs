using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x;

/// <summary>
/// A directed multigraph: parallel edges between the same endpoints and
/// self-loops are all allowed. Backed by per-vertex out/in edge lists.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public class DirectedMultigraph<TVertex, TEdge> : IDirectedGraph<TVertex, TEdge>, IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private readonly Dictionary<TVertex, List<TEdge>> _outEdges;
    private readonly Dictionary<TVertex, List<TEdge>> _inEdges;

    /// <summary>Initializes an empty multigraph using the default vertex comparer.</summary>
    public DirectedMultigraph()
        : this(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty multigraph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public DirectedMultigraph(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _outEdges = new Dictionary<TVertex, List<TEdge>>(vertexComparer);
        _inEdges = new Dictionary<TVertex, List<TEdge>>(vertexComparer);
    }

    /// <inheritdoc/>
    public int VertexCount => _outEdges.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public bool IsDirected => true;

    /// <inheritdoc/>
    public bool AllowsParallelEdges => true;

    /// <inheritdoc/>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <inheritdoc/>
    public IEnumerable<TVertex> Vertices => _outEdges.Keys;

    /// <inheritdoc/>
    public IEnumerable<TEdge> Edges
    {
        get
        {
            foreach (var outgoing in _outEdges.Values)
            {
                foreach (var edge in outgoing)
                {
                    yield return edge;
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool AddVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (_outEdges.ContainsKey(vertex))
        {
            return false;
        }

        _outEdges.Add(vertex, []);
        _inEdges.Add(vertex, []);
        return true;
    }

    /// <inheritdoc/>
    public bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        _outEdges[edge.Source].Add(edge);
        _inEdges[edge.Target].Add(edge);
        EdgeCount++;
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!_outEdges.TryGetValue(vertex, out var outgoing))
        {
            return false;
        }

        foreach (var edge in outgoing)
        {
            _inEdges[edge.Target].Remove(edge);
        }

        var incoming = _inEdges[vertex];
        foreach (var edge in incoming)
        {
            _outEdges[edge.Source].Remove(edge);
        }

        EdgeCount -= outgoing.Count + incoming.Count;
        _outEdges.Remove(vertex);
        _inEdges.Remove(vertex);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_outEdges.TryGetValue(edge.Source, out var outgoing) || !outgoing.Remove(edge))
        {
            return false;
        }

        _inEdges[edge.Target].Remove(edge);
        EdgeCount--;
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _outEdges.Clear();
        _inEdges.Clear();
        EdgeCount = 0;
    }

    /// <inheritdoc/>
    public bool ContainsVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _outEdges.ContainsKey(vertex);
    }

    /// <inheritdoc/>
    public bool ContainsEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _outEdges.TryGetValue(source, out var outgoing)
            && outgoing.Any(edge => VertexComparer.Equals(edge.Target, target));
    }

    /// <summary>Gets every parallel edge from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="source">The source endpoint.</param>
    /// <param name="target">The target endpoint.</param>
    /// <returns>The matching edges, possibly empty.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not in the graph.</exception>
    public IEnumerable<TEdge> GetEdges(TVertex source, TVertex target)
    {
        Guard.VertexExists(_outEdges, source);
        ArgumentNullException.ThrowIfNull(target);
        return _outEdges[source].Where(edge => VertexComparer.Equals(edge.Target, target));
    }

    /// <inheritdoc/>
    public int Degree(TVertex vertex) => OutDegree(vertex) + InDegree(vertex);

    /// <inheritdoc/>
    public int OutDegree(TVertex vertex)
    {
        Guard.VertexExists(_outEdges, vertex);
        return _outEdges[vertex].Count;
    }

    /// <inheritdoc/>
    public int InDegree(TVertex vertex)
    {
        Guard.VertexExists(_inEdges, vertex);
        return _inEdges[vertex].Count;
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> OutEdges(TVertex vertex)
    {
        Guard.VertexExists(_outEdges, vertex);
        return _outEdges[vertex];
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> InEdges(TVertex vertex)
    {
        Guard.VertexExists(_inEdges, vertex);
        return _inEdges[vertex];
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex)
    {
        Guard.VertexExists(_outEdges, vertex);
        return EnumerateAdjacent(vertex);
    }

    private IEnumerable<TEdge> EnumerateAdjacent(TVertex vertex)
    {
        foreach (var edge in _outEdges[vertex])
        {
            yield return edge;
        }

        foreach (var edge in _inEdges[vertex])
        {
            // Self-loops already came out of the outgoing side.
            if (!VertexComparer.Equals(edge.Source, vertex))
            {
                yield return edge;
            }
        }
    }
}
