using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Lazy breadth-first and depth-first traversals over any graph. All
/// traversals are implemented iteratively, so arbitrarily deep graphs cannot
/// overflow the call stack, and results are streamed on demand.
/// </summary>
public static class GraphTraversalExtensions
{
    /// <summary>
    /// Enumerates the vertices reachable from <paramref name="start"/> in
    /// breadth-first order. On directed graphs only out-edges are followed.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="start">The vertex to start from.</param>
    /// <returns>A lazy sequence of vertices, each visited once.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> is not in the graph.</exception>
    public static IEnumerable<TVertex> BreadthFirstSearch<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex start)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        GraphTraversalCore.ValidateStart(graph, start);
        return Iterator();

        IEnumerable<TVertex> Iterator()
        {
            var visited = new HashSet<TVertex>(graph.VertexComparer) { start };
            var queue = new Queue<TVertex>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;

                foreach (var successor in GraphTraversalCore.Successors(graph, current))
                {
                    if (visited.Add(successor))
                    {
                        queue.Enqueue(successor);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enumerates the vertices reachable from <paramref name="start"/> in
    /// depth-first pre-order (a vertex is yielded when first discovered).
    /// On directed graphs only out-edges are followed.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="start">The vertex to start from.</param>
    /// <returns>A lazy sequence of vertices, each visited once.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> is not in the graph.</exception>
    public static IEnumerable<TVertex> DepthFirstSearch<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex start)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        GraphTraversalCore.ValidateStart(graph, start);
        return Iterator();

        IEnumerable<TVertex> Iterator()
        {
            var visited = new HashSet<TVertex>(graph.VertexComparer) { start };
            var stack = new Stack<IEnumerator<TVertex>>();
            yield return start;
            stack.Push(GraphTraversalCore.Successors(graph, start).GetEnumerator());

            while (stack.Count > 0)
            {
                var successors = stack.Peek();
                if (!successors.MoveNext())
                {
                    successors.Dispose();
                    stack.Pop();
                    continue;
                }

                var next = successors.Current;
                if (visited.Add(next))
                {
                    yield return next;
                    stack.Push(GraphTraversalCore.Successors(graph, next).GetEnumerator());
                }
            }
        }
    }

    /// <summary>
    /// Enumerates the vertices reachable from <paramref name="start"/> in
    /// depth-first post-order (a vertex is yielded after all its descendants).
    /// On directed graphs only out-edges are followed.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="start">The vertex to start from.</param>
    /// <returns>A lazy sequence of vertices, each visited once.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> is not in the graph.</exception>
    public static IEnumerable<TVertex> DepthFirstSearchPostOrder<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex start)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        GraphTraversalCore.ValidateStart(graph, start);
        return Iterator();

        IEnumerable<TVertex> Iterator()
        {
            var visited = new HashSet<TVertex>(graph.VertexComparer) { start };
            var stack = new Stack<(TVertex Vertex, IEnumerator<TVertex> Successors)>();
            stack.Push((start, GraphTraversalCore.Successors(graph, start).GetEnumerator()));

            while (stack.Count > 0)
            {
                var (vertex, successors) = stack.Peek();
                if (!successors.MoveNext())
                {
                    successors.Dispose();
                    stack.Pop();
                    yield return vertex;
                    continue;
                }

                var next = successors.Current;
                if (visited.Add(next))
                {
                    stack.Push((next, GraphTraversalCore.Successors(graph, next).GetEnumerator()));
                }
            }
        }
    }
}
