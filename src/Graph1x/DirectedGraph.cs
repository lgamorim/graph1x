using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x;

/// <summary>
/// A simple directed graph (no parallel edges, self-loops allowed) backed by
/// adjacency lists with separate out- and in-edge indexes for O(1) edge lookup
/// in either direction.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public class DirectedGraph<TVertex, TEdge> : IDirectedGraph<TVertex, TEdge>, IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private readonly Dictionary<TVertex, Dictionary<TVertex, TEdge>> _outEdges;
    private readonly Dictionary<TVertex, Dictionary<TVertex, TEdge>> _inEdges;

    /// <summary>Initializes an empty graph using the default vertex comparer.</summary>
    public DirectedGraph()
        : this(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty graph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public DirectedGraph(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _outEdges = new Dictionary<TVertex, Dictionary<TVertex, TEdge>>(vertexComparer);
        _inEdges = new Dictionary<TVertex, Dictionary<TVertex, TEdge>>(vertexComparer);
    }

    /// <inheritdoc/>
    public int VertexCount => _outEdges.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public bool IsDirected => true;

    /// <inheritdoc/>
    public bool AllowsParallelEdges => false;

    /// <inheritdoc/>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <inheritdoc/>
    public IEnumerable<TVertex> Vertices => _outEdges.Keys;

    /// <inheritdoc/>
    public IEnumerable<TEdge> Edges
    {
        get
        {
            foreach (var adjacency in _outEdges.Values)
            {
                foreach (var edge in adjacency.Values)
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

        _outEdges.Add(vertex, new Dictionary<TVertex, TEdge>(VertexComparer));
        _inEdges.Add(vertex, new Dictionary<TVertex, TEdge>(VertexComparer));
        return true;
    }

    /// <inheritdoc/>
    public bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        if (_outEdges[edge.Source].ContainsKey(edge.Target))
        {
            return false;
        }

        _outEdges[edge.Source].Add(edge.Target, edge);
        _inEdges[edge.Target].Add(edge.Source, edge);
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

        foreach (var target in outgoing.Keys)
        {
            _inEdges[target].Remove(vertex);
        }

        foreach (var source in _inEdges[vertex].Keys)
        {
            _outEdges[source].Remove(vertex);
        }

        EdgeCount -= outgoing.Count + _inEdges[vertex].Count(pair => !VertexComparer.Equals(pair.Key, vertex));
        _outEdges.Remove(vertex);
        _inEdges.Remove(vertex);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_outEdges.TryGetValue(edge.Source, out var adjacency)
            || !adjacency.TryGetValue(edge.Target, out var stored)
            || !EqualityComparer<TEdge>.Default.Equals(stored, edge))
        {
            return false;
        }

        return RemoveEdge(edge.Source, edge.Target);
    }

    /// <summary>Removes the edge from <paramref name="source"/> to <paramref name="target"/>, whatever its payload.</summary>
    /// <param name="source">The source endpoint.</param>
    /// <param name="target">The target endpoint.</param>
    /// <returns><see langword="true"/> if an edge was removed.</returns>
    public bool RemoveEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!_outEdges.TryGetValue(source, out var adjacency) || !adjacency.Remove(target))
        {
            return false;
        }

        _inEdges[target].Remove(source);
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
        return _outEdges.TryGetValue(source, out var adjacency) && adjacency.ContainsKey(target);
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
        return _outEdges[vertex].Values;
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> InEdges(TVertex vertex)
    {
        Guard.VertexExists(_inEdges, vertex);
        return _inEdges[vertex].Values;
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex)
    {
        Guard.VertexExists(_outEdges, vertex);
        return EnumerateAdjacent(vertex);
    }

    private IEnumerable<TEdge> EnumerateAdjacent(TVertex vertex)
    {
        foreach (var edge in _outEdges[vertex].Values)
        {
            yield return edge;
        }

        foreach (var (source, edge) in _inEdges[vertex])
        {
            // Self-loops already came out of the outgoing side.
            if (!VertexComparer.Equals(source, vertex))
            {
                yield return edge;
            }
        }
    }
}
