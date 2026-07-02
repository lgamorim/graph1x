using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Prim's minimum-spanning-tree algorithm (lazy variant): each tree grows from
/// a root by repeatedly taking the cheapest edge crossing the frontier, using
/// a priority queue. Every connected component gets its own tree, so
/// disconnected graphs yield a spanning forest; negative weights are fine.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class PrimMinimumSpanningTree<TVertex, TEdge, TWeight> : IMinimumSpanningTreeAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public PrimMinimumSpanningTree(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
    }

    /// <inheritdoc/>
    public IReadOnlyList<TEdge> FindMinimumSpanningForest(IReadOnlyGraph<TVertex, TEdge> graph)
    {
        MinimumSpanningTreeGuard.RequireUndirected(graph);

        var comparer = graph.VertexComparer;
        var inTree = new HashSet<TVertex>(comparer);
        var forest = new List<TEdge>();
        var frontier = new PriorityQueue<(TEdge Edge, TVertex Endpoint), TWeight>();

        foreach (var root in graph.Vertices)
        {
            if (!inTree.Add(root))
            {
                continue;
            }

            Expand(graph, root, frontier);

            while (frontier.TryDequeue(out var candidate, out _))
            {
                if (!inTree.Add(candidate.Endpoint))
                {
                    continue;
                }

                forest.Add(candidate.Edge);
                Expand(graph, candidate.Endpoint, frontier);
            }
        }

        return forest;
    }

    private void Expand(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex,
        PriorityQueue<(TEdge Edge, TVertex Endpoint), TWeight> frontier)
    {
        foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, vertex))
        {
            if (!graph.VertexComparer.Equals(neighbor, vertex))
            {
                frontier.Enqueue((edge, neighbor), _weightSelector(edge));
            }
        }
    }
}
