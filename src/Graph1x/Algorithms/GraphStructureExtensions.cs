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
}
