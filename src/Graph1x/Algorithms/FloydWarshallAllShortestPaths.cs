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
    {
        ArgumentNullException.ThrowIfNull(graph);

        var vertices = graph.Vertices.ToArray();
        var count = vertices.Length;
        var index = new Dictionary<TVertex, int>(graph.VertexComparer);
        for (var i = 0; i < count; i++)
        {
            index[vertices[i]] = i;
        }

        var dist = new TWeight[count, count];
        var reachable = new bool[count, count];
        var next = new int[count, count];

        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                next[i, j] = -1;
            }

            dist[i, i] = TWeight.Zero;
            reachable[i, i] = true;
            next[i, i] = i;
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
                        next[i, j] = next[i, k];
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

        return new AllPairsShortestPaths<TVertex, TWeight>(vertices, index, dist, reachable, next);

        void RelaxArc(int source, int target, TWeight weight)
        {
            // Keep the cheapest arc when parallel edges (or a shorter self-loop) exist.
            if (!reachable[source, target] || weight < dist[source, target])
            {
                dist[source, target] = weight;
                reachable[source, target] = true;
                next[source, target] = target;
            }
        }
    }
}

/// <summary>
/// The result of a Floyd-Warshall computation: shortest distances and paths
/// between every pair of vertices, queryable via <see cref="Between"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public sealed class AllPairsShortestPaths<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly TVertex[] _vertices;
    private readonly Dictionary<TVertex, int> _index;
    private readonly TWeight[,] _dist;
    private readonly bool[,] _reachable;
    private readonly int[,] _next;

    internal AllPairsShortestPaths(
        TVertex[] vertices,
        Dictionary<TVertex, int> index,
        TWeight[,] dist,
        bool[,] reachable,
        int[,] next)
    {
        _vertices = vertices;
        _index = index;
        _dist = dist;
        _reachable = reachable;
        _next = next;
    }

    /// <summary>Gets the shortest path from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="source">The start vertex.</param>
    /// <param name="target">The end vertex.</param>
    /// <returns>The query result, unreachable when no path exists.</returns>
    /// <exception cref="ArgumentException">Either vertex was not part of the analyzed graph.</exception>
    public ShortestPathResult<TVertex, TWeight> Between(TVertex source, TVertex target)
    {
        var from = IndexOf(source, nameof(source));
        var to = IndexOf(target, nameof(target));

        if (!_reachable[from, to])
        {
            return new ShortestPathResult<TVertex, TWeight>(source, target);
        }

        var path = new List<TVertex> { _vertices[from] };
        var current = from;
        while (current != to)
        {
            current = _next[current, to];
            path.Add(_vertices[current]);
        }

        return new ShortestPathResult<TVertex, TWeight>(source, target, _dist[from, to], path);
    }

    private int IndexOf(TVertex vertex, string paramName)
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        return _index.TryGetValue(vertex, out var position)
            ? position
            : throw new ArgumentException($"Vertex '{vertex}' was not part of the analyzed graph.", paramName);
    }
}
