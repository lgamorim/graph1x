using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Johnson's all-pairs shortest-path algorithm: one Bellman-Ford pass (from an
/// implicit virtual source) establishes vertex potentials that make every
/// reduced weight non-negative, then a Dijkstra run per source fills the
/// distance table. On sparse graphs this is far cheaper than Floyd-Warshall's
/// O(V³) while supporting the same negative edge weights; any negative cycle
/// throws <see cref="NegativeCycleException"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class JohnsonAllShortestPaths<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public JohnsonAllShortestPaths(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
    }

    /// <summary>Computes shortest paths between every pair of vertices.</summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <returns>A queryable all-pairs result.</returns>
    /// <exception cref="NegativeCycleException">The graph contains a negative cycle.</exception>
    public AllPairsShortestPaths<TVertex, TWeight> Compute(IReadOnlyGraph<TVertex, TEdge> graph)
        => Compute(graph, CancellationToken.None);

    /// <summary>
    /// Computes shortest paths between every pair of vertices, observing
    /// <paramref name="cancellationToken"/> between per-source runs.
    /// </summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>A queryable all-pairs result.</returns>
    /// <exception cref="NegativeCycleException">The graph contains a negative cycle.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public AllPairsShortestPaths<TVertex, TWeight> Compute(
        IReadOnlyGraph<TVertex, TEdge> graph,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();

        var workspace = new Workspace(graph, _weightSelector);
        for (var source = 0; source < workspace.Count; source++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            workspace.ComputeRow(source);
        }

        return workspace.BuildResult();
    }

    /// <summary>
    /// Computes shortest paths between every pair of vertices, running the
    /// per-source Dijkstra passes in parallel. Results are identical to the
    /// sequential overload (each row is the same computation).
    /// </summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <param name="parallelOptions">Degree of parallelism and cancellation.</param>
    /// <returns>A queryable all-pairs result.</returns>
    /// <exception cref="NegativeCycleException">The graph contains a negative cycle.</exception>
    /// <exception cref="OperationCanceledException">The options' token was cancelled.</exception>
    public AllPairsShortestPaths<TVertex, TWeight> Compute(
        IReadOnlyGraph<TVertex, TEdge> graph,
        ParallelOptions parallelOptions)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(parallelOptions);
        parallelOptions.CancellationToken.ThrowIfCancellationRequested();

        var workspace = new Workspace(graph, _weightSelector);
        Parallel.For(0, workspace.Count, parallelOptions, workspace.ComputeRow);

        return workspace.BuildResult();
    }

    /// <summary>
    /// The shared per-computation state: potentials from the Bellman-Ford
    /// phase, the reduced-weight adjacency, and the result matrices. Rows are
    /// written independently, so <see cref="ComputeRow"/> is safe to call for
    /// distinct sources from parallel threads.
    /// </summary>
    private sealed class Workspace
    {
        private readonly TVertex[] _vertices;
        private readonly Dictionary<TVertex, int> _index;
        private readonly List<(int Target, TWeight ReducedWeight)>[] _adjacency;
        private readonly TWeight[] _potential;
        private readonly TWeight[,] _dist;
        private readonly bool[,] _reachable;
        private readonly int[,] _predecessor;

        internal Workspace(IReadOnlyGraph<TVertex, TEdge> graph, Func<TEdge, TWeight> weightSelector)
        {
            _vertices = graph.Vertices.ToArray();
            var count = _vertices.Length;
            _index = new Dictionary<TVertex, int>(count, graph.VertexComparer);
            for (var i = 0; i < count; i++)
            {
                _index[_vertices[i]] = i;
            }

            var arcs = new List<(int From, int To, TWeight Weight)>(graph.EdgeCount);
            foreach (var edge in graph.Edges)
            {
                var weight = weightSelector(edge);
                var from = _index[edge.Source];
                var to = _index[edge.Target];
                arcs.Add((from, to, weight));
                if (!graph.IsDirected && from != to)
                {
                    arcs.Add((to, from, weight));
                }
            }

            _potential = ComputePotentials(count, arcs);

            _adjacency = new List<(int, TWeight)>[count];
            for (var i = 0; i < count; i++)
            {
                _adjacency[i] = [];
            }

            foreach (var (from, to, weight) in arcs)
            {
                _adjacency[from].Add((to, weight + _potential[from] - _potential[to]));
            }

            _dist = new TWeight[count, count];
            _reachable = new bool[count, count];
            _predecessor = new int[count, count];
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < count; j++)
                {
                    _predecessor[i, j] = -1;
                }
            }
        }

        internal int Count => _vertices.Length;

        /// <summary>
        /// Dijkstra over reduced weights from one source, writing that
        /// source's row of the distance/reachability/predecessor matrices.
        /// </summary>
        internal void ComputeRow(int source)
        {
            var count = _vertices.Length;
            var distance = new TWeight[count];
            var reached = new bool[count];
            var settled = new bool[count];
            var parent = new int[count];
            var queue = new PriorityQueue<int, TWeight>(count);

            reached[source] = true;
            parent[source] = source;
            queue.Enqueue(source, TWeight.Zero);

            while (queue.TryDequeue(out var current, out _))
            {
                if (settled[current])
                {
                    continue;
                }

                settled[current] = true;
                foreach (var (target, reducedWeight) in _adjacency[current])
                {
                    if (settled[target])
                    {
                        continue;
                    }

                    var candidate = distance[current] + reducedWeight;
                    if (!reached[target] || candidate < distance[target])
                    {
                        reached[target] = true;
                        distance[target] = candidate;
                        parent[target] = current;
                        queue.Enqueue(target, candidate);
                    }
                }
            }

            // Un-reweight into real distances. The Dijkstra parents are this
            // row's own shortest-path tree, which is exactly what row-local
            // reconstruction needs: rows may break ties differently, so a hop
            // pointer from one row must never be followed into another.
            _dist[source, source] = TWeight.Zero;
            _reachable[source, source] = true;
            _predecessor[source, source] = source;
            for (var vertex = 0; vertex < count; vertex++)
            {
                if (vertex == source || !settled[vertex])
                {
                    continue;
                }

                _dist[source, vertex] = distance[vertex] + _potential[vertex] - _potential[source];
                _reachable[source, vertex] = true;
                _predecessor[source, vertex] = parent[vertex];
            }
        }

        internal AllPairsShortestPaths<TVertex, TWeight> BuildResult()
            => new(_vertices, _index, _dist, _reachable, _predecessor);

        /// <summary>
        /// Bellman-Ford from an implicit virtual source connected to every
        /// vertex with weight zero (so all initial distances are zero and the
        /// graph never gets mutated); a relaxation in the final pass means a
        /// negative cycle.
        /// </summary>
        private static TWeight[] ComputePotentials(int count, List<(int From, int To, TWeight Weight)> arcs)
        {
            var potential = new TWeight[count];
            for (var pass = 0; pass <= count; pass++)
            {
                var relaxed = false;
                foreach (var (from, to, weight) in arcs)
                {
                    var candidate = potential[from] + weight;
                    if (candidate < potential[to])
                    {
                        if (pass == count)
                        {
                            throw new NegativeCycleException(
                                "A negative-weight cycle was detected; shortest distances are undefined.");
                        }

                        potential[to] = candidate;
                        relaxed = true;
                    }
                }

                if (!relaxed)
                {
                    break;
                }
            }

            return potential;
        }
    }
}
