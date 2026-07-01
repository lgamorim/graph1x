using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Connectivity queries: connected components (edge direction ignored),
/// weak connectivity for directed graphs, and strongly connected components
/// via an iterative Tarjan algorithm.
/// </summary>
public static class GraphConnectivityExtensions
{
    /// <summary>
    /// Computes the connected components of the graph, ignoring edge direction
    /// (for directed graphs this is weak connectivity).
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to partition.</param>
    /// <returns>One vertex set per component.</returns>
    public static IReadOnlyList<IReadOnlySet<TVertex>> ConnectedComponents<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var comparer = graph.VertexComparer;
        var visited = new HashSet<TVertex>(comparer);
        var components = new List<IReadOnlySet<TVertex>>();

        foreach (var root in graph.Vertices)
        {
            if (visited.Contains(root))
            {
                continue;
            }

            var component = new HashSet<TVertex>(comparer) { root };
            visited.Add(root);
            var queue = new Queue<TVertex>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var edge in graph.AdjacentEdges(current))
                {
                    var other = GraphTraversalCore.OtherEndpoint(graph, edge, current);
                    if (visited.Add(other))
                    {
                        component.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    /// <summary>
    /// Determines whether the graph is connected when edge direction is
    /// ignored. The empty graph is trivially connected.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns><see langword="true"/> if there is at most one component.</returns>
    public static bool IsConnected<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ConnectedComponents().Count <= 1;

    /// <summary>
    /// Computes the weakly connected components of a directed graph: the
    /// connected components after forgetting edge direction.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to partition.</param>
    /// <returns>One vertex set per component.</returns>
    public static IReadOnlyList<IReadOnlySet<TVertex>> WeaklyConnectedComponents<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ConnectedComponents();

    /// <summary>
    /// Computes the strongly connected components of a directed graph using
    /// Tarjan's algorithm (iterative, stack-safe on deep graphs). Components
    /// are emitted in reverse topological order of the condensation.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to partition.</param>
    /// <returns>One vertex set per strongly connected component.</returns>
    public static IReadOnlyList<IReadOnlySet<TVertex>> StronglyConnectedComponents<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var comparer = graph.VertexComparer;
        var indices = new Dictionary<TVertex, int>(comparer);
        var lowlinks = new Dictionary<TVertex, int>(comparer);
        var onStack = new HashSet<TVertex>(comparer);
        var sccStack = new Stack<TVertex>();
        var components = new List<IReadOnlySet<TVertex>>();
        var nextIndex = 0;

        foreach (var root in graph.Vertices)
        {
            if (indices.ContainsKey(root))
            {
                continue;
            }

            var frames = new Stack<(TVertex Vertex, IEnumerator<TEdge> Edges)>();
            Discover(root);
            frames.Push((root, graph.OutEdges(root).GetEnumerator()));

            while (frames.Count > 0)
            {
                var (vertex, edges) = frames.Peek();
                if (edges.MoveNext())
                {
                    var target = edges.Current.Target;
                    if (!indices.TryGetValue(target, out var targetIndex))
                    {
                        Discover(target);
                        frames.Push((target, graph.OutEdges(target).GetEnumerator()));
                    }
                    else if (onStack.Contains(target))
                    {
                        lowlinks[vertex] = Math.Min(lowlinks[vertex], targetIndex);
                    }

                    continue;
                }

                edges.Dispose();
                frames.Pop();

                if (frames.Count > 0)
                {
                    var parent = frames.Peek().Vertex;
                    lowlinks[parent] = Math.Min(lowlinks[parent], lowlinks[vertex]);
                }

                if (lowlinks[vertex] == indices[vertex])
                {
                    var component = new HashSet<TVertex>(comparer);
                    TVertex member;
                    do
                    {
                        member = sccStack.Pop();
                        onStack.Remove(member);
                        component.Add(member);
                    }
                    while (!comparer.Equals(member, vertex));

                    components.Add(component);
                }
            }
        }

        return components;

        void Discover(TVertex vertex)
        {
            indices[vertex] = nextIndex;
            lowlinks[vertex] = nextIndex;
            nextIndex++;
            sccStack.Push(vertex);
            onStack.Add(vertex);
        }
    }
}
