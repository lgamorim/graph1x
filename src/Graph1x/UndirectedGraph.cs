using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x;

/// <summary>
/// A simple undirected graph (no parallel edges, self-loops allowed) backed by
/// adjacency lists. Edge endpoint order is irrelevant: an edge a-b is visible
/// from both endpoints and equals the connection b-a.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public class UndirectedGraph<TVertex, TEdge> : IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    // Each edge is indexed under both endpoints (once for self-loops).
    private readonly Dictionary<TVertex, Dictionary<TVertex, TEdge>> _adjacency;

    /// <summary>Initializes an empty graph using the default vertex comparer.</summary>
    public UndirectedGraph()
        : this(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty graph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public UndirectedGraph(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _adjacency = new Dictionary<TVertex, Dictionary<TVertex, TEdge>>(vertexComparer);
    }

    /// <inheritdoc/>
    public int VertexCount => _adjacency.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public bool IsDirected => false;

    /// <inheritdoc/>
    public bool AllowsParallelEdges => false;

    /// <inheritdoc/>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <inheritdoc/>
    public IEnumerable<TVertex> Vertices => _adjacency.Keys;

    /// <inheritdoc/>
    public IEnumerable<TEdge> Edges
    {
        get
        {
            foreach (var (vertex, adjacency) in _adjacency)
            {
                foreach (var edge in adjacency.Values)
                {
                    // Every edge is indexed under both endpoints; yield it only
                    // from its source endpoint so each edge comes out once.
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
        if (_adjacency.ContainsKey(vertex))
        {
            return false;
        }

        _adjacency.Add(vertex, new Dictionary<TVertex, TEdge>(VertexComparer));
        return true;
    }

    /// <inheritdoc/>
    public bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        if (_adjacency[edge.Source].ContainsKey(edge.Target))
        {
            return false;
        }

        _adjacency[edge.Source].Add(edge.Target, edge);
        if (!VertexComparer.Equals(edge.Source, edge.Target))
        {
            _adjacency[edge.Target].Add(edge.Source, edge);
        }

        EdgeCount++;
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!_adjacency.TryGetValue(vertex, out var adjacency))
        {
            return false;
        }

        foreach (var neighbor in adjacency.Keys)
        {
            if (!VertexComparer.Equals(neighbor, vertex))
            {
                _adjacency[neighbor].Remove(vertex);
            }
        }

        EdgeCount -= adjacency.Count;
        _adjacency.Remove(vertex);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_adjacency.TryGetValue(edge.Source, out var adjacency)
            || !adjacency.TryGetValue(edge.Target, out var stored)
            || !EqualityComparer<TEdge>.Default.Equals(stored, edge))
        {
            return false;
        }

        return RemoveEdge(edge.Source, edge.Target);
    }

    /// <summary>Removes the edge between <paramref name="source"/> and <paramref name="target"/>, whatever its payload or stored orientation.</summary>
    /// <param name="source">One endpoint.</param>
    /// <param name="target">The other endpoint.</param>
    /// <returns><see langword="true"/> if an edge was removed.</returns>
    public bool RemoveEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!_adjacency.TryGetValue(source, out var adjacency) || !adjacency.Remove(target))
        {
            return false;
        }

        if (!VertexComparer.Equals(source, target))
        {
            _adjacency[target].Remove(source);
        }

        EdgeCount--;
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _adjacency.Clear();
        EdgeCount = 0;
    }

    /// <inheritdoc/>
    public bool ContainsVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _adjacency.ContainsKey(vertex);
    }

    /// <inheritdoc/>
    public bool ContainsEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _adjacency.TryGetValue(source, out var adjacency) && adjacency.ContainsKey(target);
    }

    /// <inheritdoc/>
    public int Degree(TVertex vertex)
    {
        Guard.VertexExists(_adjacency, vertex);
        var adjacency = _adjacency[vertex];

        // A self-loop contributes two endpoints to the degree but is indexed once.
        return adjacency.Count + (adjacency.ContainsKey(vertex) ? 1 : 0);
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex)
    {
        Guard.VertexExists(_adjacency, vertex);
        return _adjacency[vertex].Values;
    }
}
