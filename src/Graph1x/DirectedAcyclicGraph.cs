using Graph1x.Edges;

namespace Graph1x;

/// <summary>
/// A directed graph that maintains acyclicity as an invariant: adding an edge
/// that would create a cycle (including a self-loop) is rejected with
/// <see langword="false"/>, mirroring how duplicates are rejected.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public class DirectedAcyclicGraph<TVertex, TEdge> : DirectedGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Initializes an empty DAG using the default vertex comparer.</summary>
    public DirectedAcyclicGraph()
    {
    }

    /// <summary>Initializes an empty DAG using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public DirectedAcyclicGraph(IEqualityComparer<TVertex> vertexComparer)
        : base(vertexComparer)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Also returns <see langword="false"/> when the edge would create a cycle,
    /// i.e. when its source is already reachable from its target.
    /// </remarks>
    public override bool AddEdge(TEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (VertexComparer.Equals(edge.Source, edge.Target))
        {
            return false;
        }

        if (ContainsVertex(edge.Source) && ContainsVertex(edge.Target) && IsReachable(edge.Target, edge.Source))
        {
            return false;
        }

        return base.AddEdge(edge);
    }

    private bool IsReachable(TVertex from, TVertex to)
    {
        var visited = new HashSet<TVertex>(VertexComparer);
        var stack = new Stack<TVertex>();
        stack.Push(from);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (VertexComparer.Equals(current, to))
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var edge in OutEdges(current))
            {
                stack.Push(edge.Target);
            }
        }

        return false;
    }
}
