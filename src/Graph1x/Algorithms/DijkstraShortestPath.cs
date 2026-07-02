using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Dijkstra's shortest-path algorithm over a <see cref="PriorityQueue{TElement, TPriority}"/>.
/// Requires non-negative edge weights; throws <see cref="NegativeWeightException"/> otherwise.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class DijkstraShortestPath<TVertex, TEdge, TWeight> : IShortestPathAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public DijkstraShortestPath(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
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

        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(comparer);
        var settled = new HashSet<TVertex>(comparer);
        var frontier = new PriorityQueue<TVertex, TWeight>();
        frontier.Enqueue(source, TWeight.Zero);

        while (frontier.TryDequeue(out var current, out _))
        {
            if (!settled.Add(current))
            {
                continue;
            }

            if (comparer.Equals(current, target))
            {
                break;
            }

            foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                var weight = _weightSelector(edge);
                if (weight < TWeight.Zero)
                {
                    throw new NegativeWeightException(
                        $"Edge '{edge}' has negative weight {weight}; Dijkstra requires non-negative weights. Use Bellman-Ford instead.");
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
                    frontier.Enqueue(neighbor, candidate);
                }
            }
        }

        return !distance.TryGetValue(target, out var total) || !settled.Contains(target)
            ? new ShortestPathResult<TVertex, TWeight>(source, target)
            : new ShortestPathResult<TVertex, TWeight>(
                source,
                target,
                total,
                GraphTraversalCore.BuildPath(source, target, predecessor, comparer));
    }
}
