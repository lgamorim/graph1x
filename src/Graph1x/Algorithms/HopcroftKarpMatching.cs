using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// The Hopcroft-Karp maximum-cardinality matching algorithm for undirected
/// bipartite graphs, running in O(E·√V) by augmenting along maximal sets of
/// shortest vertex-disjoint paths per phase. The bipartition is derived
/// automatically; non-bipartite input is rejected. No strategy interface is
/// introduced: with a single matching algorithm there is no swap point to
/// justify one.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed class HopcroftKarpMatching<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Computes a maximum-cardinality matching of the bipartite graph.</summary>
    /// <param name="graph">The undirected bipartite graph.</param>
    /// <returns>The matched edges; no two share a vertex, and no larger matching exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="graph"/> is directed or not bipartite.</exception>
    public IReadOnlyList<TEdge> FindMaximumMatching(IReadOnlyGraph<TVertex, TEdge> graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsDirected)
        {
            throw new ArgumentException("Bipartite matching is defined for undirected graphs.", nameof(graph));
        }

        var partition = graph.FindBipartition()
            ?? throw new ArgumentException("The graph is not bipartite, so bipartite matching does not apply.", nameof(graph));

        var comparer = graph.VertexComparer;

        // Adjacency from the left side, deduplicating parallel edges and
        // remembering one representative edge per (left, right) pair.
        var pairEdges = new Dictionary<TVertex, Dictionary<TVertex, TEdge>>(comparer);
        foreach (var left in partition.Left)
        {
            pairEdges[left] = new Dictionary<TVertex, TEdge>(comparer);
        }

        foreach (var edge in graph.Edges)
        {
            var (left, right) = partition.Left.Contains(edge.Source)
                ? (edge.Source, edge.Target)
                : (edge.Target, edge.Source);
            pairEdges[left].TryAdd(right, edge);
        }

        var state = new MatchingState(comparer, pairEdges);
        while (state.BuildLayers(partition.Left))
        {
            foreach (var left in partition.Left)
            {
                if (!state.IsMatched(left))
                {
                    state.TryAugment(left);
                }
            }
        }

        return state.MatchedEdges();
    }

    /// <summary>Mutable matching state shared by the BFS layering and DFS augmentation phases.</summary>
    private sealed class MatchingState(
        IEqualityComparer<TVertex> comparer,
        Dictionary<TVertex, Dictionary<TVertex, TEdge>> pairEdges)
    {
        private const int Infinity = int.MaxValue;

        private readonly Dictionary<TVertex, TVertex> _matchLeft = new(comparer);
        private readonly Dictionary<TVertex, TVertex> _matchRight = new(comparer);
        private readonly Dictionary<TVertex, int> _distance = new(comparer);
        private int _freeLayerDistance;

        internal bool IsMatched(TVertex left) => _matchLeft.ContainsKey(left);

        /// <summary>
        /// BFS phase: layers left vertices by alternating-path length from the
        /// free ones. Returns <see langword="false"/> when no augmenting path exists.
        /// </summary>
        internal bool BuildLayers(IReadOnlySet<TVertex> leftSide)
        {
            _distance.Clear();
            _freeLayerDistance = Infinity;

            var queue = new Queue<TVertex>();
            foreach (var left in leftSide)
            {
                if (!_matchLeft.ContainsKey(left))
                {
                    _distance[left] = 0;
                    queue.Enqueue(left);
                }
            }

            while (queue.Count > 0)
            {
                var left = queue.Dequeue();
                if (_distance[left] >= _freeLayerDistance)
                {
                    continue;
                }

                foreach (var right in pairEdges[left].Keys)
                {
                    if (!_matchRight.TryGetValue(right, out var nextLeft))
                    {
                        if (_freeLayerDistance == Infinity)
                        {
                            _freeLayerDistance = _distance[left] + 1;
                        }
                    }
                    else if (!_distance.ContainsKey(nextLeft))
                    {
                        _distance[nextLeft] = _distance[left] + 1;
                        queue.Enqueue(nextLeft);
                    }
                }
            }

            return _freeLayerDistance != Infinity;
        }

        /// <summary>
        /// DFS phase (iterative): augments along one shortest alternating path
        /// starting at the free left vertex, if the layered graph admits one.
        /// </summary>
        internal bool TryAugment(TVertex root)
        {
            var frames = new Stack<Frame>();
            frames.Push(new Frame(root, pairEdges[root].Keys.GetEnumerator()));

            while (frames.Count > 0)
            {
                var frame = frames.Peek();
                var descended = false;

                while (frame.Rights.MoveNext())
                {
                    var right = frame.Rights.Current;
                    if (!_matchRight.TryGetValue(right, out var nextLeft))
                    {
                        if (DistanceOf(frame.Left) + 1 == _freeLayerDistance)
                        {
                            // Free right vertex on the shortest layer: flip the
                            // whole path captured by the frame stack.
                            _matchLeft[frame.Left] = right;
                            _matchRight[right] = frame.Left;
                            frames.Pop();
                            while (frames.Count > 0)
                            {
                                var parent = frames.Pop();
                                _matchLeft[parent.Left] = parent.PendingRight!;
                                _matchRight[parent.PendingRight!] = parent.Left;
                            }

                            return true;
                        }
                    }
                    else if (DistanceOf(nextLeft) == DistanceOf(frame.Left) + 1)
                    {
                        frame.PendingRight = right;
                        frames.Push(new Frame(nextLeft, pairEdges[nextLeft].Keys.GetEnumerator()));
                        descended = true;
                        break;
                    }
                }

                if (descended)
                {
                    continue;
                }

                // Exhausted: this left vertex is a dead end for this phase.
                _distance[frame.Left] = Infinity;
                frames.Pop();
            }

            return false;
        }

        internal IReadOnlyList<TEdge> MatchedEdges()
            => _matchLeft.Select(pair => pairEdges[pair.Key][pair.Value]).ToList();

        private int DistanceOf(TVertex left)
            => _distance.TryGetValue(left, out var distance) ? distance : Infinity;

        private sealed class Frame(TVertex left, IEnumerator<TVertex> rights)
        {
            internal TVertex Left { get; } = left;

            internal IEnumerator<TVertex> Rights { get; } = rights;

            internal TVertex? PendingRight { get; set; }
        }
    }
}
