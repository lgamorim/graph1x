using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// Shared machinery for adjacency-matrix graphs: dense storage with O(1) edge
/// lookup, suited to dense graphs where V² cells are acceptable. The matrix
/// grows by doubling; removing a vertex moves the last row/column into the
/// vacated slot so the matrix stays compact. Simple-graph semantics (no
/// parallel edges, self-loops allowed). The hierarchy is closed: use
/// <see cref="DirectedAdjacencyMatrixGraph{TVertex, TEdge}"/> or
/// <see cref="UndirectedAdjacencyMatrixGraph{TVertex, TEdge}"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public abstract class AdjacencyMatrixGraphBase<TVertex, TEdge> : IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private const int InitialCapacity = 4;

    private readonly Dictionary<TVertex, int> _index;
    private readonly List<TVertex> _ordinals;
    private bool[,] _has;
    private TEdge[,] _matrix;

    private protected AdjacencyMatrixGraphBase(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _index = new Dictionary<TVertex, int>(vertexComparer);
        _ordinals = [];
        _has = new bool[InitialCapacity, InitialCapacity];
        _matrix = new TEdge[InitialCapacity, InitialCapacity];
    }

    /// <inheritdoc/>
    public int VertexCount => _index.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    /// <inheritdoc/>
    public abstract bool IsDirected { get; }

    /// <inheritdoc/>
    public bool AllowsParallelEdges => false;

    /// <inheritdoc/>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <inheritdoc/>
    public IEnumerable<TVertex> Vertices => _index.Keys;

    /// <inheritdoc/>
    public IEnumerable<TEdge> Edges
    {
        get
        {
            for (var row = 0; row < VertexCount; row++)
            {
                for (var column = 0; column < VertexCount; column++)
                {
                    // Undirected edges live in two symmetric cells; the
                    // row <= column half covers each edge exactly once.
                    if (_has[row, column] && (IsDirected || row <= column))
                    {
                        yield return _matrix[row, column];
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool AddVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (_index.ContainsKey(vertex))
        {
            return false;
        }

        EnsureCapacity(VertexCount + 1);
        _index.Add(vertex, VertexCount);
        _ordinals.Add(vertex);
        return true;
    }

    /// <inheritdoc/>
    public bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        var source = _index[edge.Source];
        var target = _index[edge.Target];
        if (_has[source, target])
        {
            return false;
        }

        SetCell(source, target, edge);
        if (!IsDirected)
        {
            SetCell(target, source, edge);
        }

        EdgeCount++;
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!_index.TryGetValue(vertex, out var removed))
        {
            return false;
        }

        var last = VertexCount - 1;

        var incident = 0;
        for (var j = 0; j <= last; j++)
        {
            if (_has[removed, j])
            {
                incident++;
            }
        }

        if (IsDirected)
        {
            for (var i = 0; i <= last; i++)
            {
                if (_has[i, removed])
                {
                    incident++;
                }
            }

            if (_has[removed, removed])
            {
                incident--; // the self-loop sat in both the row and the column
            }
        }

        EdgeCount -= incident;

        for (var j = 0; j <= last; j++)
        {
            ClearCell(removed, j);
            ClearCell(j, removed);
        }

        if (removed != last)
        {
            for (var j = 0; j <= last; j++)
            {
                MoveCell(from: (last, j), to: (removed, j));
            }

            for (var i = 0; i <= last; i++)
            {
                MoveCell(from: (i, last), to: (i, removed));
            }

            var moved = _ordinals[last];
            _index[moved] = removed;
            _ordinals[removed] = moved;
        }

        _index.Remove(vertex);
        _ordinals.RemoveAt(last);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_index.TryGetValue(edge.Source, out var source)
            || !_index.TryGetValue(edge.Target, out var target)
            || !_has[source, target]
            || !EqualityComparer<TEdge>.Default.Equals(_matrix[source, target], edge))
        {
            return false;
        }

        return RemoveEdge(edge.Source, edge.Target);
    }

    /// <summary>Removes the edge between <paramref name="source"/> and <paramref name="target"/>, whatever its payload.</summary>
    /// <param name="source">The source endpoint (either endpoint on undirected graphs).</param>
    /// <param name="target">The target endpoint.</param>
    /// <returns><see langword="true"/> if an edge was removed.</returns>
    public bool RemoveEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!_index.TryGetValue(source, out var from)
            || !_index.TryGetValue(target, out var to)
            || !_has[from, to])
        {
            return false;
        }

        ClearCell(from, to);
        if (!IsDirected)
        {
            ClearCell(to, from);
        }

        EdgeCount--;
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _index.Clear();
        _ordinals.Clear();
        _has = new bool[InitialCapacity, InitialCapacity];
        _matrix = new TEdge[InitialCapacity, InitialCapacity];
        EdgeCount = 0;
    }

    /// <inheritdoc/>
    public bool ContainsVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _index.ContainsKey(vertex);
    }

    /// <inheritdoc/>
    public bool ContainsEdge(TVertex source, TVertex target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _index.TryGetValue(source, out var from)
            && _index.TryGetValue(target, out var to)
            && _has[from, to];
    }

    /// <inheritdoc/>
    public int Degree(TVertex vertex)
    {
        var position = IndexOf(vertex);
        if (IsDirected)
        {
            return CountRow(position) + CountColumn(position);
        }

        // A self-loop occupies one symmetric cell but contributes two endpoints.
        return CountRow(position) + (_has[position, position] ? 1 : 0);
    }

    /// <inheritdoc/>
    public IEnumerable<TEdge> AdjacentEdges(TVertex vertex)
    {
        var position = IndexOf(vertex);
        return IsDirected
            ? EdgesInRow(position).Concat(EdgesInColumn(position, skipDiagonal: true))
            : EdgesInRow(position);
    }

    private protected int IndexOf(TVertex vertex, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(vertex))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        return _index.TryGetValue(vertex, out var position)
            ? position
            : throw new ArgumentException($"Vertex '{vertex}' is not in the graph.", paramName);
    }

    private protected int CountRow(int row)
    {
        var count = 0;
        for (var j = 0; j < VertexCount; j++)
        {
            if (_has[row, j])
            {
                count++;
            }
        }

        return count;
    }

    private protected int CountColumn(int column)
    {
        var count = 0;
        for (var i = 0; i < VertexCount; i++)
        {
            if (_has[i, column])
            {
                count++;
            }
        }

        return count;
    }

    private protected IEnumerable<TEdge> EdgesInRow(int row)
    {
        for (var j = 0; j < VertexCount; j++)
        {
            if (_has[row, j])
            {
                yield return _matrix[row, j];
            }
        }
    }

    private protected IEnumerable<TEdge> EdgesInColumn(int column, bool skipDiagonal = false)
    {
        for (var i = 0; i < VertexCount; i++)
        {
            if (_has[i, column] && !(skipDiagonal && i == column))
            {
                yield return _matrix[i, column];
            }
        }
    }

    private void SetCell(int row, int column, TEdge edge)
    {
        _has[row, column] = true;
        _matrix[row, column] = edge;
    }

    private void ClearCell(int row, int column)
    {
        _has[row, column] = false;
        _matrix[row, column] = default!;
    }

    private void MoveCell((int Row, int Column) from, (int Row, int Column) to)
    {
        _has[to.Row, to.Column] = _has[from.Row, from.Column];
        _matrix[to.Row, to.Column] = _matrix[from.Row, from.Column];
        ClearCell(from.Row, from.Column);
    }

    private void EnsureCapacity(int required)
    {
        var capacity = _has.GetLength(0);
        if (required <= capacity)
        {
            return;
        }

        var expanded = Math.Max(capacity * 2, required);
        var has = new bool[expanded, expanded];
        var matrix = new TEdge[expanded, expanded];
        for (var i = 0; i < VertexCount; i++)
        {
            for (var j = 0; j < VertexCount; j++)
            {
                has[i, j] = _has[i, j];
                matrix[i, j] = _matrix[i, j];
            }
        }

        _has = has;
        _matrix = matrix;
    }
}
