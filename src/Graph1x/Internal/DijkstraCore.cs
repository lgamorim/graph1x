using System.Numerics;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.Internal;

/// <summary>
/// The Dijkstra relaxation loop shared by the public algorithm class and the
/// spur searches of k-shortest-path enumeration, which differ only in which
/// arcs they may take.
/// </summary>
internal static class DijkstraCore
{
    /// <summary>
    /// Runs Dijkstra from <paramref name="source"/>, stopping as soon as
    /// <paramref name="target"/> is settled when <paramref name="hasTarget"/>
    /// is set. Arcs for which <paramref name="skipArc"/> (current, neighbor)
    /// returns <see langword="true"/> are ignored. A target has a distance
    /// entry on return exactly when it is reachable: the early exit happens
    /// only once it is settled, and an exhausted frontier settles every entry.
    /// </summary>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered; <paramref name="requirement"/> completes the message.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled between vertex settlements.</exception>
    internal static (Dictionary<TVertex, TWeight> Distance, Dictionary<TVertex, TVertex> Predecessor) Relax<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        TVertex source,
        bool hasTarget,
        TVertex? target,
        Func<TVertex, TVertex, bool>? skipArc,
        string requirement,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var comparer = graph.VertexComparer;
        var capacity = graph.VertexCount;
        var distance = new Dictionary<TVertex, TWeight>(capacity, comparer) { [source] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(capacity, comparer);
        var settled = new HashSet<TVertex>(capacity, comparer);
        var frontier = new PriorityQueue<TVertex, TWeight>(capacity);
        frontier.Enqueue(source, TWeight.Zero);

        while (frontier.TryDequeue(out var current, out _))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!settled.Add(current))
            {
                continue;
            }

            if (hasTarget && comparer.Equals(current, target!))
            {
                break;
            }

            foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                var weight = weightSelector(edge);
                if (weight < TWeight.Zero)
                {
                    throw new NegativeWeightException(
                        $"Edge '{edge}' has negative weight {weight}; {requirement}");
                }

                if (settled.Contains(neighbor) || (skipArc?.Invoke(current, neighbor) ?? false))
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

        return (distance, predecessor);
    }
}
