using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// The Floyd-Warshall all-pairs shortest-path algorithm. Supports negative
/// edge weights; throws <see cref="NegativeCycleException"/> when any negative
/// cycle exists. Reachability is tracked explicitly, so weight types without
/// an infinity value (int, decimal, ...) work unchanged.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class FloydWarshallAllShortestPaths<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _weightSelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's weight.</summary>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <exception cref="ArgumentNullException"><paramref name="weightSelector"/> is <see langword="null"/>.</exception>
    public FloydWarshallAllShortestPaths(Func<TEdge, TWeight> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(weightSelector);
        _weightSelector = weightSelector;
    }

    /// <summary>Computes shortest paths between every pair of vertices.</summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <returns>A queryable all-pairs result.</returns>
    /// <exception cref="NegativeCycleException">The graph contains a negative cycle.</exception>
    public AllPairsShortestPaths<TVertex, TWeight> Compute(IReadOnlyGraph<TVertex, TEdge> graph)
        => Compute(graph, CancellationToken.None);

    /// <summary>
    /// Computes shortest paths between every pair of vertices, observing
    /// <paramref name="cancellationToken"/> between pivot iterations.
    /// </summary>
    /// <param name="graph">The graph to analyze.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>A queryable all-pairs result.</returns>
    /// <exception cref="NegativeCycleException">The graph contains a negative cycle.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public AllPairsShortestPaths<TVertex, TWeight> Compute(
        IReadOnlyGraph<TVertex, TEdge> graph,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();

        var vertices = graph.Vertices.ToArray();
        var count = vertices.Length;
        var index = new Dictionary<TVertex, int>(graph.VertexComparer);
        for (var i = 0; i < count; i++)
        {
            index[vertices[i]] = i;
        }

        var dist = new TWeight[count, count];
        var reachable = new bool[count, count];
        var predecessor = new int[count, count];

        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                predecessor[i, j] = -1;
            }

            dist[i, i] = TWeight.Zero;
            reachable[i, i] = true;
            predecessor[i, i] = i;
        }

        foreach (var edge in graph.Edges)
        {
            var weight = _weightSelector(edge);
            var source = index[edge.Source];
            var target = index[edge.Target];
            RelaxArc(source, target, weight);
            if (!graph.IsDirected)
            {
                RelaxArc(target, source, weight);
            }
        }

        for (var k = 0; k < count; k++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var i = 0; i < count; i++)
            {
                if (!reachable[i, k])
                {
                    continue;
                }

                for (var j = 0; j < count; j++)
                {
                    if (!reachable[k, j])
                    {
                        continue;
                    }

                    var candidate = dist[i, k] + dist[k, j];
                    if (!reachable[i, j] || candidate < dist[i, j])
                    {
                        dist[i, j] = candidate;
                        reachable[i, j] = true;
                        predecessor[i, j] = predecessor[k, j];
                    }
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            if (dist[i, i] < TWeight.Zero)
            {
                throw new NegativeCycleException(
                    $"A negative-weight cycle through '{vertices[i]}' was detected; shortest distances are undefined.");
            }
        }

        return new AllPairsShortestPaths<TVertex, TWeight>(vertices, index, dist, reachable, predecessor);

        void RelaxArc(int source, int target, TWeight weight)
        {
            // Keep the cheapest arc when parallel edges (or a shorter self-loop) exist.
            if (!reachable[source, target] || weight < dist[source, target])
            {
                dist[source, target] = weight;
                reachable[source, target] = true;
                predecessor[source, target] = source;
            }
        }
    }
}
