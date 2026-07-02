using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Topological ordering of directed acyclic graphs using Kahn's algorithm.
/// </summary>
public static class GraphTopologicalSortExtensions
{
    /// <summary>
    /// Computes a topological order of the graph: every edge's source appears
    /// before its target. Ties are broken by vertex insertion order (FIFO), so
    /// the result is deterministic for a given construction sequence.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to sort.</param>
    /// <returns>The vertices in topological order.</returns>
    /// <exception cref="GraphCycleException">The graph contains a cycle; the exception carries one offending cycle.</exception>
    public static IReadOnlyList<TVertex> TopologicalSort<TVertex, TEdge>(this IDirectedGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var inDegree = new Dictionary<TVertex, int>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            inDegree[vertex] = graph.InDegree(vertex);
        }

        var ready = new Queue<TVertex>(graph.Vertices.Where(vertex => inDegree[vertex] == 0));
        var order = new List<TVertex>(graph.VertexCount);

        while (ready.Count > 0)
        {
            var vertex = ready.Dequeue();
            order.Add(vertex);

            foreach (var edge in graph.OutEdges(vertex))
            {
                if (--inDegree[edge.Target] == 0)
                {
                    ready.Enqueue(edge.Target);
                }
            }
        }

        if (order.Count < graph.VertexCount)
        {
            var cycle = graph.FindCycle();
            throw new GraphCycleException(
                "The graph contains at least one cycle and cannot be topologically sorted.",
                cycle?.Cast<object>().ToArray() ?? []);
        }

        return order;
    }
}
