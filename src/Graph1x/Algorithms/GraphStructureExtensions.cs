using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Structural queries: density, degree sequence, bipartiteness, and transpose.
/// </summary>
public static class GraphStructureExtensions
{
    /// <summary>
    /// Computes the graph's density: the ratio of existing edges to the
    /// maximum possible between distinct vertices (E / V(V-1) directed,
    /// 2E / V(V-1) undirected). Graphs with fewer than two vertices have
    /// density 0; self-loops and parallel edges can push it above 1.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>The density.</returns>
    public static double Density<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.VertexCount < 2)
        {
            return 0.0;
        }

        double possible = (double)graph.VertexCount * (graph.VertexCount - 1);
        var actual = graph.IsDirected ? graph.EdgeCount : 2.0 * graph.EdgeCount;
        return actual / possible;
    }

    /// <summary>Gets the graph's degree sequence in descending order.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Every vertex's degree, largest first.</returns>
    public static IReadOnlyList<int> DegreeSequence<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph.Vertices.Select(graph.Degree).OrderDescending().ToList();
    }

    /// <summary>Determines whether the graph is bipartite (edge direction ignored).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns><see langword="true"/> if the vertices admit a proper 2-coloring.</returns>
    public static bool IsBipartite<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.FindBipartition() is not null;

    /// <summary>
    /// Finds a bipartition of the graph (edge direction ignored): two vertex
    /// sets such that every edge crosses between them.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to partition.</param>
    /// <returns>The two sets, or <see langword="null"/> when the graph is not bipartite.</returns>
    public static (IReadOnlySet<TVertex> Left, IReadOnlySet<TVertex> Right)? FindBipartition<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var comparer = graph.VertexComparer;
        var left = new HashSet<TVertex>(comparer);
        var right = new HashSet<TVertex>(comparer);
        var queue = new Queue<TVertex>();

        foreach (var root in graph.Vertices)
        {
            if (left.Contains(root) || right.Contains(root))
            {
                continue;
            }

            left.Add(root);
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentInLeft = left.Contains(current);

                foreach (var edge in graph.AdjacentEdges(current))
                {
                    var other = GraphTraversalCore.OtherEndpoint(graph, edge, current);
                    if (comparer.Equals(other, current))
                    {
                        return null; // self-loop: no proper 2-coloring exists
                    }

                    var otherInLeft = left.Contains(other);
                    var otherInRight = right.Contains(other);
                    if (!otherInLeft && !otherInRight)
                    {
                        (currentInLeft ? right : left).Add(other);
                        queue.Enqueue(other);
                    }
                    else if (otherInLeft == currentInLeft)
                    {
                        return null; // odd cycle
                    }
                }
            }
        }

        return (left, right);
    }

    /// <summary>
    /// Builds the transpose of a directed graph: same vertices, every edge
    /// reversed via <paramref name="reverseEdge"/>. The result preserves the
    /// source's parallel-edge policy.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to transpose.</param>
    /// <param name="reverseEdge">Builds the reversed counterpart of an edge.</param>
    /// <returns>A new directed graph with all edges reversed.</returns>
    public static IDirectedGraph<TVertex, TEdge> Transpose<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        Func<TEdge, TEdge> reverseEdge)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(reverseEdge);

        IMutableGraph<TVertex, TEdge> transposed = graph.AllowsParallelEdges
            ? new DirectedMultigraph<TVertex, TEdge>(graph.VertexComparer)
            : new DirectedGraph<TVertex, TEdge>(graph.VertexComparer);

        foreach (var vertex in graph.Vertices)
        {
            transposed.AddVertex(vertex);
        }

        foreach (var edge in graph.Edges)
        {
            transposed.AddEdge(reverseEdge(edge));
        }

        return (IDirectedGraph<TVertex, TEdge>)transposed;
    }

    /// <summary>Builds the transpose of a directed graph with <see cref="Edge{TVertex}"/> edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="graph">The directed graph to transpose.</param>
    /// <returns>A new directed graph with all edges reversed.</returns>
    public static IDirectedGraph<TVertex, Edge<TVertex>> Transpose<TVertex>(
        this IDirectedGraph<TVertex, Edge<TVertex>> graph)
        where TVertex : notnull
        => graph.Transpose(edge => new Edge<TVertex>(edge.Target, edge.Source));

    /// <summary>Builds the transpose of a directed graph with <see cref="WeightedEdge{TVertex, TWeight}"/> edges, keeping weights.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The directed graph to transpose.</param>
    /// <returns>A new directed graph with all edges reversed.</returns>
    public static IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> Transpose<TVertex, TWeight>(
        this IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.Transpose(edge => new WeightedEdge<TVertex, TWeight>(edge.Target, edge.Source, edge.Weight));

    /// <summary>
    /// Builds the transitive closure of a directed graph: an edge u → v for
    /// every non-empty path u → ... → v in the original (so vertices on cycles
    /// gain self-loops). Vertices are preserved.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to close.</param>
    /// <param name="edgeFactory">Builds the closure edge for a (source, target) pair.</param>
    /// <returns>A new directed graph containing the closure.</returns>
    public static IDirectedGraph<TVertex, TEdge> TransitiveClosure<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        Func<TVertex, TVertex, TEdge> edgeFactory)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.TransitiveClosure(edgeFactory, CancellationToken.None);

    /// <summary>
    /// Builds the transitive closure, observing
    /// <paramref name="cancellationToken"/> between source vertices.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to close.</param>
    /// <param name="edgeFactory">Builds the closure edge for a (source, target) pair.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>A new directed graph containing the closure.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IDirectedGraph<TVertex, TEdge> TransitiveClosure<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(edgeFactory);
        cancellationToken.ThrowIfCancellationRequested();

        var comparer = graph.VertexComparer;
        var closure = new DirectedGraph<TVertex, TEdge>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            closure.AddVertex(vertex);
        }

        foreach (var source in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // BFS over out-edges; the source itself only enters `reached` when
            // an edge leads back to it, which is exactly the cycle case.
            var reached = new HashSet<TVertex>(comparer);
            var queue = new Queue<TVertex>();
            foreach (var edge in graph.OutEdges(source))
            {
                if (reached.Add(edge.Target))
                {
                    queue.Enqueue(edge.Target);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                closure.AddEdge(edgeFactory(source, current));
                foreach (var edge in graph.OutEdges(current))
                {
                    if (reached.Add(edge.Target))
                    {
                        queue.Enqueue(edge.Target);
                    }
                }
            }
        }

        return closure;
    }

    /// <summary>Builds the transitive closure of a directed graph with <see cref="Edge{TVertex}"/> edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="graph">The directed graph to close.</param>
    /// <returns>A new directed graph containing the closure.</returns>
    public static IDirectedGraph<TVertex, Edge<TVertex>> TransitiveClosure<TVertex>(
        this IDirectedGraph<TVertex, Edge<TVertex>> graph)
        where TVertex : notnull
        => graph.TransitiveClosure((source, target) => new Edge<TVertex>(source, target));

    /// <summary>
    /// Builds the transitive reduction of a directed acyclic graph: the unique
    /// minimal edge set with the same reachability. An edge u → v is dropped
    /// when v is also reachable through another successor of u. Parallel edges
    /// collapse to one; vertices are preserved.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed acyclic graph to reduce.</param>
    /// <returns>A new directed graph containing the reduction.</returns>
    /// <exception cref="GraphCycleException">The graph contains a cycle; general graphs have no unique reduction.</exception>
    public static IDirectedGraph<TVertex, TEdge> TransitiveReduction<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.TransitiveReduction(CancellationToken.None);

    /// <summary>
    /// Builds the transitive reduction of a DAG, observing
    /// <paramref name="cancellationToken"/> between vertices.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed acyclic graph to reduce.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>A new directed graph containing the reduction.</returns>
    /// <exception cref="GraphCycleException">The graph contains a cycle.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IDirectedGraph<TVertex, TEdge> TransitiveReduction<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        var order = graph.TopologicalSort(); // throws GraphCycleException on cyclic input

        var comparer = graph.VertexComparer;

        // Descendant sets in reverse topological order: every successor is
        // processed before the vertices pointing at it.
        var descendants = new Dictionary<TVertex, HashSet<TVertex>>(comparer);
        for (var i = order.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var vertex = order[i];
            var reachable = new HashSet<TVertex>(comparer);
            foreach (var edge in graph.OutEdges(vertex))
            {
                reachable.Add(edge.Target);
                reachable.UnionWith(descendants[edge.Target]);
            }

            descendants[vertex] = reachable;
        }

        var reduced = new DirectedGraph<TVertex, TEdge>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            reduced.AddVertex(vertex);
        }

        foreach (var vertex in graph.Vertices)
        {
            var successors = new HashSet<TVertex>(graph.OutEdges(vertex).Select(edge => edge.Target), comparer);
            foreach (var edge in graph.OutEdges(vertex))
            {
                var redundant = successors.Any(other =>
                    !comparer.Equals(other, edge.Target) && descendants[other].Contains(edge.Target));
                if (!redundant)
                {
                    reduced.AddEdge(edge);
                }
            }
        }

        return reduced;
    }
}
