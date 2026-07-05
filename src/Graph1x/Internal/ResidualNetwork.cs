using System.Numerics;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.Internal;

/// <summary>Shared argument validation for maximum-flow strategies.</summary>
internal static class FlowGuards
{
    internal static void Validate<TVertex, TEdge>(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));
        GraphTraversalCore.ValidateEndpoint(graph, sink, nameof(sink));
        if (graph.VertexComparer.Equals(source, sink))
        {
            throw new ArgumentException("Source and sink must be distinct vertices.", nameof(sink));
        }
    }
}

/// <summary>
/// The indexed residual network shared by the maximum-flow strategies: arcs
/// are stored flat, with each forward arc at an even id and its zero-capacity
/// reverse arc at id + 1, so the partner of arc <c>a</c> is always
/// <c>a ^ 1</c>. Parallel and anti-parallel edges each get their own arc
/// pair; self-loops are skipped; negative capacities are rejected.
/// </summary>
internal sealed class ResidualNetwork<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly IEqualityComparer<TVertex> _comparer;
    private readonly Dictionary<TVertex, int> _index;
    private readonly TVertex[] _vertices;
    private readonly List<int>[] _incidentArcs;
    private readonly List<int> _arcHead = [];
    private readonly List<TWeight> _capacity = [];
    private readonly List<TWeight> _flow = [];
    private readonly List<TEdge> _origin = [];

    internal ResidualNetwork(IDirectedGraph<TVertex, TEdge> graph, Func<TEdge, TWeight> capacitySelector)
    {
        _comparer = graph.VertexComparer;
        _vertices = graph.Vertices.ToArray();
        _index = new Dictionary<TVertex, int>(_comparer);
        for (var i = 0; i < _vertices.Length; i++)
        {
            _index[_vertices[i]] = i;
        }

        _incidentArcs = new List<int>[_vertices.Length];
        for (var i = 0; i < _vertices.Length; i++)
        {
            _incidentArcs[i] = [];
        }

        foreach (var edge in graph.Edges)
        {
            var capacity = capacitySelector(edge);
            if (capacity < TWeight.Zero)
            {
                throw new NegativeWeightException(
                    $"Edge '{edge}' has negative capacity {capacity}; flow networks require non-negative capacities.");
            }

            if (_comparer.Equals(edge.Source, edge.Target))
            {
                continue; // self-loops cannot carry source-to-sink flow
            }

            AddArcPair(_index[edge.Source], _index[edge.Target], capacity, edge);
        }
    }

    internal int VertexCount => _vertices.Length;

    internal int IndexOf(TVertex vertex) => _index[vertex];

    internal IReadOnlyList<int> IncidentArcs(int vertex) => _incidentArcs[vertex];

    internal int Head(int arc) => _arcHead[arc];

    internal TWeight Residual(int arc) => _capacity[arc] - _flow[arc];

    internal void Push(int arc, TWeight amount)
    {
        _flow[arc] += amount;
        _flow[arc ^ 1] -= amount;
    }

    /// <summary>BFS for a shortest augmenting path (Edmonds-Karp); returns per-vertex incoming arcs, or null when the sink is unreachable.</summary>
    internal int[]? FindAugmentingPath(int source, int sink)
    {
        var parentArc = new int[_vertices.Length];
        Array.Fill(parentArc, -1);
        var visited = new bool[_vertices.Length];
        visited[source] = true;

        var queue = new Queue<int>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var arc in _incidentArcs[current])
            {
                var head = _arcHead[arc];
                if (visited[head] || Residual(arc) <= TWeight.Zero)
                {
                    continue;
                }

                visited[head] = true;
                parentArc[head] = arc;
                if (head == sink)
                {
                    return parentArc;
                }

                queue.Enqueue(head);
            }
        }

        return null;
    }

    /// <summary>Pushes the bottleneck residual along the found path and returns it.</summary>
    internal TWeight Augment(int[] parentArc, int source, int sink)
    {
        var bottleneck = Residual(parentArc[sink]);
        for (var v = _arcHead[parentArc[sink] ^ 1]; v != source; v = _arcHead[parentArc[v] ^ 1])
        {
            bottleneck = TWeight.Min(bottleneck, Residual(parentArc[v]));
        }

        for (var v = sink; v != source; v = _arcHead[parentArc[v] ^ 1])
        {
            Push(parentArc[v], bottleneck);
        }

        return bottleneck;
    }

    /// <summary>BFS over positive-residual arcs, marking the source side of the minimum cut.</summary>
    internal bool[] ResidualReachableFrom(int source)
    {
        var reachable = new bool[_vertices.Length];
        reachable[source] = true;
        var queue = new Queue<int>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var arc in _incidentArcs[current])
            {
                var head = _arcHead[arc];
                if (!reachable[head] && Residual(arc) > TWeight.Zero)
                {
                    reachable[head] = true;
                    queue.Enqueue(head);
                }
            }
        }

        return reachable;
    }

    /// <summary>Assembles the result: per-edge flows plus the certifying minimum cut.</summary>
    internal MaximumFlowResult<TVertex, TEdge, TWeight> BuildResult(TVertex source, TVertex sink, TWeight total)
    {
        var reachable = ResidualReachableFrom(IndexOf(source));
        var sourceSide = new HashSet<TVertex>(_comparer);
        for (var i = 0; i < _vertices.Length; i++)
        {
            if (reachable[i])
            {
                sourceSide.Add(_vertices[i]);
            }
        }

        var edgeFlows = new List<(TEdge Edge, TWeight Flow)>();
        var cutEdges = new List<TEdge>();
        for (var arc = 0; arc < _arcHead.Count; arc += 2)
        {
            var tail = _arcHead[arc + 1];
            var head = _arcHead[arc];
            edgeFlows.Add((_origin[arc], _flow[arc]));
            if (reachable[tail] && !reachable[head])
            {
                cutEdges.Add(_origin[arc]);
            }
        }

        return new MaximumFlowResult<TVertex, TEdge, TWeight>(
            source, sink, total, edgeFlows, sourceSide, cutEdges);
    }

    private void AddArcPair(int tail, int head, TWeight capacity, TEdge origin)
    {
        _incidentArcs[tail].Add(_arcHead.Count);
        _arcHead.Add(head);
        _capacity.Add(capacity);
        _flow.Add(TWeight.Zero);
        _origin.Add(origin);

        _incidentArcs[head].Add(_arcHead.Count);
        _arcHead.Add(tail);
        _capacity.Add(TWeight.Zero);
        _flow.Add(TWeight.Zero);
        _origin.Add(origin);
    }
}
