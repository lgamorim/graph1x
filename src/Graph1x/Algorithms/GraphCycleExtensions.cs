using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Cycle detection for directed graphs (three-color depth-first search) and
/// undirected graphs (depth-first search with parent-edge tracking, so parallel
/// edges in multigraphs are correctly recognized as cycles).
/// </summary>
public static class GraphCycleExtensions
{
    /// <summary>Determines whether the graph contains at least one cycle.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns><see langword="true"/> if a cycle exists.</returns>
    public static bool HasCycle<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.FindCycle() is not null;

    /// <summary>
    /// Finds a cycle in the graph, if any. The result lists the cycle's
    /// vertices in order: each vertex connects to the next, and the last
    /// connects back to the first. A self-loop yields a single-vertex cycle.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns>The cycle's vertices, or <see langword="null"/> when the graph is acyclic.</returns>
    public static IReadOnlyList<TVertex>? FindCycle<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph is IDirectedGraph<TVertex, TEdge> directed
            ? FindDirectedCycle(directed)
            : FindUndirectedCycle(graph);
    }

    private static IReadOnlyList<TVertex>? FindDirectedCycle<TVertex, TEdge>(IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        const byte Gray = 1;
        const byte Black = 2;
        var state = new Dictionary<TVertex, byte>(graph.VertexComparer);
        var parent = new Dictionary<TVertex, TVertex>(graph.VertexComparer);

        foreach (var root in graph.Vertices)
        {
            if (state.ContainsKey(root))
            {
                continue;
            }

            state[root] = Gray;
            var stack = new Stack<(TVertex Vertex, IEnumerator<TEdge> Edges)>();
            stack.Push((root, graph.OutEdges(root).GetEnumerator()));

            while (stack.Count > 0)
            {
                var (vertex, edges) = stack.Peek();
                if (!edges.MoveNext())
                {
                    edges.Dispose();
                    stack.Pop();
                    state[vertex] = Black;
                    continue;
                }

                var target = edges.Current.Target;
                if (!state.TryGetValue(target, out var targetState))
                {
                    state[target] = Gray;
                    parent[target] = vertex;
                    stack.Push((target, graph.OutEdges(target).GetEnumerator()));
                }
                else if (targetState == Gray)
                {
                    return ReconstructCycle(vertex, target, parent, graph.VertexComparer);
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<TVertex>? FindUndirectedCycle<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var visited = new HashSet<TVertex>(comparer);
        var parent = new Dictionary<TVertex, TVertex>(comparer);

        foreach (var root in graph.Vertices)
        {
            if (!visited.Add(root))
            {
                continue;
            }

            var stack = new Stack<UndirectedFrame<TVertex, TEdge>>();
            stack.Push(new UndirectedFrame<TVertex, TEdge>(root, graph.AdjacentEdges(root).GetEnumerator()));

            while (stack.Count > 0)
            {
                var frame = stack.Peek();
                if (!frame.Edges.MoveNext())
                {
                    frame.Edges.Dispose();
                    stack.Pop();
                    continue;
                }

                var other = GraphTraversalCore.OtherEndpoint(graph, frame.Edges.Current, frame.Vertex);
                if (comparer.Equals(other, frame.Vertex))
                {
                    return [frame.Vertex]; // self-loop
                }

                // The tree edge back to the parent is not a cycle; skip it
                // exactly once so a parallel edge to the parent still counts.
                if (!frame.SkippedParentEdge
                    && parent.TryGetValue(frame.Vertex, out var parentVertex)
                    && comparer.Equals(other, parentVertex))
                {
                    frame.SkippedParentEdge = true;
                    continue;
                }

                if (!visited.Add(other))
                {
                    return ReconstructCycle(frame.Vertex, other, parent, comparer);
                }

                parent[other] = frame.Vertex;
                stack.Push(new UndirectedFrame<TVertex, TEdge>(other, graph.AdjacentEdges(other).GetEnumerator()));
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the cycle closed by the back edge <paramref name="from"/> →
    /// <paramref name="ancestor"/>: the tree path ancestor → ... → from,
    /// which the back edge completes.
    /// </summary>
    private static List<TVertex> ReconstructCycle<TVertex>(
        TVertex from,
        TVertex ancestor,
        Dictionary<TVertex, TVertex> parent,
        IEqualityComparer<TVertex> comparer)
        where TVertex : notnull
    {
        var cycle = new List<TVertex>();
        var current = from;
        while (!comparer.Equals(current, ancestor))
        {
            cycle.Add(current);
            current = parent[current];
        }

        cycle.Add(ancestor);
        cycle.Reverse();
        return cycle;
    }

    private sealed class UndirectedFrame<TVertex, TEdge>(TVertex vertex, IEnumerator<TEdge> edges)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        public TVertex Vertex { get; } = vertex;

        public IEnumerator<TEdge> Edges { get; } = edges;

        public bool SkippedParentEdge { get; set; }
    }
}
