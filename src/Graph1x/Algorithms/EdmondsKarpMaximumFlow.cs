using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// The Edmonds-Karp maximum-flow algorithm: Ford-Fulkerson with breadth-first
/// augmenting paths, giving O(V·E²) independently of capacity values. Each
/// edge (including parallel and anti-parallel edges) gets its own residual arc
/// pair; self-loops carry no flow and are ignored. With floating-point
/// capacities, tiny rounding residues are possible — integer or decimal
/// capacities are exact.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity type.</typeparam>
public sealed class EdmondsKarpMaximumFlow<TVertex, TEdge, TWeight> : IMaximumFlowAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _capacitySelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's capacity.</summary>
    /// <param name="capacitySelector">Maps an edge to its capacity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="capacitySelector"/> is <see langword="null"/>.</exception>
    public EdmondsKarpMaximumFlow(Func<TEdge, TWeight> capacitySelector)
    {
        ArgumentNullException.ThrowIfNull(capacitySelector);
        _capacitySelector = capacitySelector;
    }

    /// <inheritdoc/>
    public MaximumFlowResult<TVertex, TEdge, TWeight> FindMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink)
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));
        GraphTraversalCore.ValidateEndpoint(graph, sink, nameof(sink));
        if (graph.VertexComparer.Equals(source, sink))
        {
            throw new ArgumentException("Source and sink must be distinct vertices.", nameof(sink));
        }

        var network = new ResidualNetwork(graph, _capacitySelector);
        var sourceIndex = network.IndexOf(source);
        var sinkIndex = network.IndexOf(sink);

        var total = TWeight.Zero;
        while (true)
        {
            var parentArc = network.FindAugmentingPath(sourceIndex, sinkIndex);
            if (parentArc is null)
            {
                break;
            }

            total += network.Augment(parentArc, sourceIndex, sinkIndex);
        }

        var reachable = network.ResidualReachableFrom(sourceIndex);
        var sourceSide = new HashSet<TVertex>(graph.VertexComparer);
        for (var i = 0; i < network.VertexCount; i++)
        {
            if (reachable[i])
            {
                sourceSide.Add(network.VertexAt(i));
            }
        }

        var edgeFlows = new List<(TEdge Edge, TWeight Flow)>();
        var cutEdges = new List<TEdge>();
        foreach (var (edge, tail, head, flow) in network.ForwardArcs())
        {
            edgeFlows.Add((edge, flow));
            if (reachable[tail] && !reachable[head])
            {
                cutEdges.Add(edge);
            }
        }

        return new MaximumFlowResult<TVertex, TEdge, TWeight>(
            source, sink, total, edgeFlows, sourceSide, cutEdges);
    }

    /// <summary>
    /// The indexed residual network: arcs are stored flat, with each forward
    /// arc at an even id and its zero-capacity reverse arc at id + 1, so the
    /// partner of arc <c>a</c> is always <c>a ^ 1</c>.
    /// </summary>
    private sealed class ResidualNetwork
    {
        private readonly Dictionary<TVertex, int> _index;
        private readonly TVertex[] _vertices;
        private readonly List<int>[] _incidentArcs;
        private readonly List<int> _arcHead = [];
        private readonly List<TWeight> _capacity = [];
        private readonly List<TWeight> _flow = [];
        private readonly List<TEdge> _origin = [];

        internal ResidualNetwork(IDirectedGraph<TVertex, TEdge> graph, Func<TEdge, TWeight> capacitySelector)
        {
            _vertices = graph.Vertices.ToArray();
            _index = new Dictionary<TVertex, int>(graph.VertexComparer);
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

                if (graph.VertexComparer.Equals(edge.Source, edge.Target))
                {
                    continue; // self-loops cannot carry source-to-sink flow
                }

                AddArcPair(_index[edge.Source], _index[edge.Target], capacity, edge);
            }
        }

        internal int VertexCount => _vertices.Length;

        internal int IndexOf(TVertex vertex) => _index[vertex];

        internal TVertex VertexAt(int index) => _vertices[index];

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

        private TWeight Residual(int arc) => _capacity[arc] - _flow[arc];

        /// <summary>BFS for a shortest augmenting path; returns the per-vertex incoming arc, or null when the sink is unreachable.</summary>
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
                _flow[parentArc[v]] += bottleneck;
                _flow[parentArc[v] ^ 1] -= bottleneck;
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

        /// <summary>Enumerates the forward arcs as (origin edge, tail index, head index, flow).</summary>
        internal IEnumerable<(TEdge Edge, int Tail, int Head, TWeight Flow)> ForwardArcs()
        {
            for (var arc = 0; arc < _arcHead.Count; arc += 2)
            {
                yield return (_origin[arc], _arcHead[arc + 1], _arcHead[arc], _flow[arc]);
            }
        }
    }
}
