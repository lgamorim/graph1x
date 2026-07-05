using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// The A* shortest-path algorithm: Dijkstra guided by a caller-supplied
/// heuristic estimating the remaining distance to the target. The heuristic
/// must be consistent (never overestimate, and satisfy the triangle
/// inequality) for the result to be optimal; a zero heuristic degrades to
/// plain Dijkstra. Requires non-negative edge weights.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class AStarShortestPath<TVertex, TEdge, TWeight> : IShortestPathAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;
    private readonly Func<TVertex, TVertex, TWeight> _heuristic;

    /// <summary>Initializes the algorithm with a weight selector and a heuristic.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <param name="heuristic">Estimates the remaining distance from a vertex (first argument) to the target (second argument).</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public AStarShortestPath(Func<TEdge, TWeight> weightSelector, Func<TVertex, TVertex, TWeight> heuristic)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        ArgumentNullException.ThrowIfNull(heuristic);
        _weightSelector = weightSelector;
        _heuristic = heuristic;
    }

    /// <inheritdoc/>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public ShortestPathResult<TVertex, TWeight> FindPath(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex target)
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));
        GraphTraversalCore.ValidateEndpoint(graph, target, nameof(target));

        // Deliberately NOT pre-sized to VertexCount: a well-guided A* visits a
        // small fraction of the graph, and benchmarking showed full-capacity
        // pre-sizing triples its allocations for zero speedup.
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(comparer);
        var settled = new HashSet<TVertex>(comparer);
        var frontier = new PriorityQueue<TVertex, TWeight>();
        frontier.Enqueue(source, _heuristic(source, target));

        while (frontier.TryDequeue(out var current, out _))
        {
            if (!settled.Add(current))
            {
                continue;
            }

            if (comparer.Equals(current, target))
            {
                return new ShortestPathResult<TVertex, TWeight>(
                    source,
                    target,
                    distance[target],
                    GraphTraversalCore.BuildPath(source, target, predecessor, comparer));
            }

            foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                var weight = _weightSelector(edge);
                if (weight < TWeight.Zero)
                {
                    throw new NegativeWeightException(
                        $"Edge '{edge}' has negative weight {weight}; A* requires non-negative weights. Use Bellman-Ford instead.");
                }

                if (settled.Contains(neighbor))
                {
                    continue;
                }

                var candidate = distance[current] + weight;
                if (!distance.TryGetValue(neighbor, out var known) || candidate < known)
                {
                    distance[neighbor] = candidate;
                    predecessor[neighbor] = current;
                    frontier.Enqueue(neighbor, candidate + _heuristic(neighbor, target));
                }
            }
        }

        return new ShortestPathResult<TVertex, TWeight>(source, target);
    }
}
