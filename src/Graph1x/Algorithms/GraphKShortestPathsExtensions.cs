using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// K-shortest-path enumeration via Yen's algorithm, exposed as a lazy
/// sequence: paths arrive in nondecreasing total weight and the caller
/// controls the cost by how far it enumerates (<c>Take(k)</c> for the classic
/// k-shortest query). Paths are simple (loopless) and vertex-distinct —
/// parallel edges only influence the weight, where the cheapest one counts.
/// </summary>
public static class GraphKShortestPathsExtensions
{
    /// <summary>
    /// Lazily enumerates the simple paths from <paramref name="source"/> to
    /// <paramref name="target"/> in nondecreasing total weight (Yen's
    /// algorithm over repeated Dijkstra runs). An unreachable target yields
    /// an empty sequence.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <param name="weightSelector">Maps an edge to its (non-negative) weight.</param>
    /// <returns>The simple paths, cheapest first.</returns>
    /// <exception cref="ArgumentException">Either endpoint is not in the graph.</exception>
    /// <exception cref="NegativeWeightException">An edge with negative weight is encountered during enumeration.</exception>
    public static IEnumerable<ShortestPathResult<TVertex, TWeight>> EnumerateShortestPaths<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex target,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        GraphTraversalCore.ValidateEndpoint(graph, source, nameof(source));
        GraphTraversalCore.ValidateEndpoint(graph, target, nameof(target));

        return Enumerate(graph, source, target, weightSelector);
    }

    /// <summary>
    /// Lazily enumerates the simple paths from <paramref name="source"/> to
    /// <paramref name="target"/> in nondecreasing total weight, using the
    /// weights carried by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <returns>The simple paths, cheapest first.</returns>
    /// <exception cref="ArgumentException">Either endpoint is not in the graph.</exception>
    /// <exception cref="NegativeWeightException">An edge with negative weight is encountered during enumeration.</exception>
    public static IEnumerable<ShortestPathResult<TVertex, TWeight>> EnumerateShortestPaths<TVertex, TWeight>(
        this IReadOnlyGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source,
        TVertex target)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.EnumerateShortestPaths(source, target, edge => edge.Weight);

    private static IEnumerable<ShortestPathResult<TVertex, TWeight>> Enumerate<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex target,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var comparer = graph.VertexComparer;
        var first = SpurDijkstra(graph, weightSelector, source, target, bannedVertices: null, bannedFirstHops: null);
        if (first is null)
        {
            yield break;
        }

        var accepted = new List<(List<TVertex> Path, TWeight Distance)> { first.Value };
        var seen = new HashSet<List<TVertex>>(new PathComparer<TVertex>(comparer)) { first.Value.Path };

        // Candidates ordered by weight; the running sequence number breaks
        // ties deterministically (insertion order).
        var candidates = new PriorityQueue<(List<TVertex> Path, TWeight Distance), (TWeight, long)>();
        var sequence = 0L;

        yield return new ShortestPathResult<TVertex, TWeight>(
            source, target, first.Value.Distance, first.Value.Path);

        while (true)
        {
            var (previousPath, _) = accepted[^1];
            var rootWeight = TWeight.Zero;
            for (var spurIndex = 0; spurIndex < previousPath.Count - 1; spurIndex++)
            {
                var spur = previousPath[spurIndex];

                var bannedFirstHops = new HashSet<TVertex>(comparer);
                foreach (var (path, _) in accepted)
                {
                    if (path.Count > spurIndex + 1 && SharesPrefix(path, previousPath, spurIndex + 1, comparer))
                    {
                        bannedFirstHops.Add(path[spurIndex + 1]);
                    }
                }

                var bannedVertices = new HashSet<TVertex>(comparer);
                for (var i = 0; i < spurIndex; i++)
                {
                    bannedVertices.Add(previousPath[i]);
                }

                var spurResult = SpurDijkstra(graph, weightSelector, spur, target, bannedVertices, bannedFirstHops);
                if (spurResult is not null)
                {
                    var candidatePath = new List<TVertex>(spurIndex + spurResult.Value.Path.Count);
                    for (var i = 0; i < spurIndex; i++)
                    {
                        candidatePath.Add(previousPath[i]);
                    }

                    candidatePath.AddRange(spurResult.Value.Path);
                    if (seen.Add(candidatePath))
                    {
                        var total = rootWeight + spurResult.Value.Distance;
                        candidates.Enqueue((candidatePath, total), (total, sequence++));
                    }
                }

                rootWeight += MinArcWeight(graph, weightSelector, spur, previousPath[spurIndex + 1]);
            }

            if (!candidates.TryDequeue(out var nextPath, out _))
            {
                yield break;
            }

            accepted.Add(nextPath);
            yield return new ShortestPathResult<TVertex, TWeight>(
                source, target, nextPath.Distance, nextPath.Path);
        }
    }

    private static bool SharesPrefix<TVertex>(
        List<TVertex> path, List<TVertex> reference, int length, IEqualityComparer<TVertex> comparer)
        where TVertex : notnull
    {
        if (path.Count < length)
        {
            return false;
        }

        for (var i = 0; i < length; i++)
        {
            if (!comparer.Equals(path[i], reference[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The cheapest arc weight between two adjacent vertices (parallel edges collapse to the cheapest).</summary>
    private static TWeight MinArcWeight<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        TVertex from,
        TVertex to)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var comparer = graph.VertexComparer;
        var best = TWeight.Zero;
        var found = false;
        foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, from))
        {
            if (!comparer.Equals(neighbor, to))
            {
                continue;
            }

            var weight = CheckedWeight(weightSelector, edge);
            if (!found || weight < best)
            {
                best = weight;
                found = true;
            }
        }

        return best; // callers only ask about consecutive path vertices, so a match always exists
    }

    /// <summary>
    /// Dijkstra from <paramref name="from"/> to <paramref name="target"/>,
    /// ignoring banned vertices entirely and banned first hops out of
    /// <paramref name="from"/> (Yen's spur computation).
    /// </summary>
    private static (List<TVertex> Path, TWeight Distance)? SpurDijkstra<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        TVertex from,
        TVertex target,
        HashSet<TVertex>? bannedVertices,
        HashSet<TVertex>? bannedFirstHops)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [from] = TWeight.Zero };
        var predecessor = new Dictionary<TVertex, TVertex>(comparer);
        var settled = new HashSet<TVertex>(comparer);
        var frontier = new PriorityQueue<TVertex, TWeight>();
        frontier.Enqueue(from, TWeight.Zero);

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

            var isSpur = comparer.Equals(current, from);
            foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                var weight = CheckedWeight(weightSelector, edge);
                if (settled.Contains(neighbor)
                    || (bannedVertices?.Contains(neighbor) ?? false)
                    || (isSpur && (bannedFirstHops?.Contains(neighbor) ?? false)))
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

        if (!distance.TryGetValue(target, out var total) || !settled.Contains(target))
        {
            return null;
        }

        var path = new List<TVertex> { target };
        var walker = target;
        while (!comparer.Equals(walker, from))
        {
            walker = predecessor[walker];
            path.Add(walker);
        }

        path.Reverse();
        return (path, total);
    }

    private static TWeight CheckedWeight<TEdge, TWeight>(Func<TEdge, TWeight> weightSelector, TEdge edge)
        where TWeight : INumber<TWeight>
    {
        var weight = weightSelector(edge);
        if (weight < TWeight.Zero)
        {
            throw new NegativeWeightException(
                $"Edge '{edge}' has negative weight {weight}; k-shortest-path enumeration requires non-negative weights.");
        }

        return weight;
    }

    /// <summary>Vertex-sequence equality under the graph's vertex comparer.</summary>
    private sealed class PathComparer<TVertex>(IEqualityComparer<TVertex> comparer) : IEqualityComparer<List<TVertex>>
        where TVertex : notnull
    {
        public bool Equals(List<TVertex>? x, List<TVertex>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (!comparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(List<TVertex> obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Count);
            foreach (var vertex in obj)
            {
                hash.Add(comparer.GetHashCode(vertex));
            }

            return hash.ToHashCode();
        }
    }
}
