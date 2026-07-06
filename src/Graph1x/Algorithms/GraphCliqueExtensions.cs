using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Maximal clique enumeration via Bron–Kerbosch with Tomita pivoting.
/// Edge direction is ignored, self-loops never count, and on multigraphs
/// neighbors are counted once regardless of parallel edges.
/// </summary>
public static class GraphCliqueExtensions
{
    /// <summary>
    /// Lazily enumerates every maximal clique: each set of mutually adjacent
    /// vertices that no further vertex can extend. Enumeration is iterative
    /// (no recursion) and streaming; because the number of maximal cliques
    /// can grow exponentially with the vertex count, the caller controls the
    /// cost by how far the sequence is enumerated — there is deliberately no
    /// <see cref="CancellationToken"/> overload, as pull-based enumeration is
    /// already cooperative.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <returns>
    /// The maximal cliques, each as a list of vertices; deterministic order
    /// for a given graph. The empty graph yields no cliques; an isolated
    /// vertex forms a single-vertex clique.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IEnumerable<IReadOnlyList<TVertex>> EnumerateMaximalCliques<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return Enumerate(graph);
    }

    private static IEnumerable<IReadOnlyList<TVertex>> Enumerate<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        if (graph.VertexCount == 0)
        {
            yield break;
        }

        var vertices = new TVertex[graph.VertexCount];
        var indices = new Dictionary<TVertex, int>(graph.VertexCount, graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            vertices[indices.Count] = vertex;
            indices[vertex] = indices.Count;
        }

        var adjacency = new HashSet<int>[vertices.Length];
        for (var i = 0; i < adjacency.Length; i++)
        {
            adjacency[i] = [];
        }

        foreach (var edge in graph.Edges)
        {
            var source = indices[edge.Source];
            var target = indices[edge.Target];
            if (source != target)
            {
                adjacency[source].Add(target);
                adjacency[target].Add(source);
            }
        }

        var rootCandidates = new HashSet<int>(Enumerable.Range(0, vertices.Length));
        var stack = new Stack<Frame>();
        stack.Push(new Frame(rootCandidates, [], adjacency, addedVertex: false));

        // The growing clique R, one vertex per non-root frame plus the
        // in-flight candidate of the frame being expanded.
        var current = new List<int>();

        while (stack.Count > 0)
        {
            var frame = stack.Peek();
            if (frame.Index == frame.Extension.Count)
            {
                stack.Pop();
                if (frame.AddedVertex)
                {
                    current.RemoveAt(current.Count - 1);
                }

                continue;
            }

            var candidate = frame.Extension[frame.Index++];
            var neighbors = adjacency[candidate];
            var childCandidates = Intersect(frame.Candidates, neighbors);
            var childExcluded = Intersect(frame.Excluded, neighbors);
            frame.Candidates.Remove(candidate);
            frame.Excluded.Add(candidate);
            current.Add(candidate);

            if (childCandidates.Count == 0 && childExcluded.Count == 0)
            {
                yield return Snapshot(current, vertices);
                current.RemoveAt(current.Count - 1);
            }
            else if (childCandidates.Count == 0)
            {
                // Some excluded vertex extends R ∪ {candidate}: not maximal.
                current.RemoveAt(current.Count - 1);
            }
            else
            {
                stack.Push(new Frame(childCandidates, childExcluded, adjacency, addedVertex: true));
            }
        }
    }

    private static HashSet<int> Intersect(HashSet<int> set, HashSet<int> other)
    {
        var (smaller, larger) = set.Count <= other.Count ? (set, other) : (other, set);
        var result = new HashSet<int>(smaller.Count);
        foreach (var item in smaller)
        {
            if (larger.Contains(item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static TVertex[] Snapshot<TVertex>(List<int> current, TVertex[] vertices)
        where TVertex : notnull
    {
        var clique = new TVertex[current.Count];
        for (var i = 0; i < current.Count; i++)
        {
            clique[i] = vertices[current[i]];
        }

        return clique;
    }

    /// <summary>
    /// One Bron–Kerbosch subproblem: the candidate set P, the excluded set X,
    /// and the pivot-pruned expansion list P \ N(pivot) walked by
    /// <see cref="Index"/>.
    /// </summary>
    private sealed class Frame
    {
        public Frame(HashSet<int> candidates, HashSet<int> excluded, HashSet<int>[] adjacency, bool addedVertex)
        {
            Candidates = candidates;
            Excluded = excluded;
            AddedVertex = addedVertex;
            Extension = ComputeExtension(candidates, excluded, adjacency);
        }

        public HashSet<int> Candidates { get; }

        public HashSet<int> Excluded { get; }

        public List<int> Extension { get; }

        public bool AddedVertex { get; }

        public int Index { get; set; }

        /// <summary>
        /// Picks the Tomita pivot — the vertex of P ∪ X whose neighborhood
        /// covers most of P (ties broken by smallest index, so enumeration is
        /// deterministic) — and returns P \ N(pivot) in ascending order.
        /// </summary>
        private static List<int> ComputeExtension(
            HashSet<int> candidates,
            HashSet<int> excluded,
            HashSet<int>[] adjacency)
        {
            var pivot = -1;
            var pivotCovered = -1;
            foreach (var vertex in candidates)
            {
                Consider(vertex);
            }

            foreach (var vertex in excluded)
            {
                Consider(vertex);
            }

            var extension = new List<int>();
            var pivotNeighbors = adjacency[pivot];
            foreach (var vertex in candidates)
            {
                if (!pivotNeighbors.Contains(vertex))
                {
                    extension.Add(vertex);
                }
            }

            extension.Sort();
            return extension;

            void Consider(int vertex)
            {
                var covered = 0;
                foreach (var neighbor in adjacency[vertex])
                {
                    if (candidates.Contains(neighbor))
                    {
                        covered++;
                    }
                }

                if (covered > pivotCovered || (covered == pivotCovered && vertex < pivot))
                {
                    pivot = vertex;
                    pivotCovered = covered;
                }
            }
        }
    }
}
