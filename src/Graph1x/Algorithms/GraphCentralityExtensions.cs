using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Centrality measures: degree, closeness (Wasserman-Faust scaled, so
/// disconnected graphs need no special casing), betweenness via Brandes'
/// algorithm (breadth-first for hop counts, Dijkstra-based for weights),
/// PageRank for directed graphs, and the spectral pair — eigenvector and
/// Katz — by power iteration. On multigraphs, parallel edges count as
/// distinct shortest paths, which is the natural multigraph semantics.
/// </summary>
public static class GraphCentralityExtensions
{
    /// <summary>Gets each vertex's degree divided by (V - 1); on directed graphs the total degree (in + out) is used.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Degree centrality per vertex (0 for a single-vertex graph).</returns>
    public static IReadOnlyDictionary<TVertex, double> DegreeCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        var scale = graph.VertexCount > 1 ? 1.0 / (graph.VertexCount - 1) : 0.0;
        var centrality = new Dictionary<TVertex, double>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            centrality[vertex] = graph.Degree(vertex) * scale;
        }

        return centrality;
    }

    /// <summary>Gets hop-count closeness centrality.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Closeness per vertex.</returns>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ClosenessCentrality(_ => 1);

    /// <summary>Gets hop-count closeness centrality, observing <paramref name="cancellationToken"/> between vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>Closeness per vertex.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ClosenessCentrality(_ => 1, cancellationToken);

    /// <summary>
    /// Gets closeness centrality from weighted distances (measured from each
    /// vertex outward), Wasserman-Faust scaled by reachable-set size so
    /// disconnected graphs yield comparable values; unreachable and isolated
    /// vertices score 0.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>Closeness per vertex.</returns>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.ClosenessCentrality(weightSelector, CancellationToken.None);

    /// <summary>Gets weighted closeness centrality, observing <paramref name="cancellationToken"/> between vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>Closeness per vertex.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        cancellationToken.ThrowIfCancellationRequested();

        var centrality = new Dictionary<TVertex, double>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            centrality[vertex] = ClosenessOf(graph, vertex, weightSelector);
        }

        return centrality;
    }

    /// <summary>
    /// Gets hop-count closeness centrality with the per-vertex sweeps running
    /// in parallel. The token inside <paramref name="parallelOptions"/>
    /// cancels cooperatively between vertices.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure; only read, never mutated.</param>
    /// <param name="parallelOptions">Degree of parallelism and cancellation.</param>
    /// <returns>Closeness per vertex, identical to the sequential result.</returns>
    /// <exception cref="OperationCanceledException">The token inside the options was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        ParallelOptions parallelOptions)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ClosenessCentrality(_ => 1, parallelOptions);

    /// <summary>
    /// Gets weighted closeness centrality with the per-vertex sweeps running
    /// in parallel. <paramref name="weightSelector"/> is invoked concurrently
    /// and must be pure. Every vertex's score is computed independently, so
    /// the result is identical to the sequential one.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure; only read, never mutated.</param>
    /// <param name="weightSelector">Maps an edge to its weight; must be safe to call concurrently.</param>
    /// <param name="parallelOptions">Degree of parallelism and cancellation.</param>
    /// <returns>Closeness per vertex, identical to the sequential result.</returns>
    /// <exception cref="OperationCanceledException">The token inside the options was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> ClosenessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        ParallelOptions parallelOptions)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        ArgumentNullException.ThrowIfNull(parallelOptions);
        parallelOptions.CancellationToken.ThrowIfCancellationRequested();

        var vertices = graph.Vertices.ToList();
        var scores = new double[vertices.Count];
        Parallel.For(
            0,
            vertices.Count,
            parallelOptions,
            index => scores[index] = ClosenessOf(graph, vertices[index], weightSelector));

        var centrality = new Dictionary<TVertex, double>(vertices.Count, graph.VertexComparer);
        for (var index = 0; index < vertices.Count; index++)
        {
            centrality[vertices[index]] = scores[index];
        }

        return centrality;
    }

    private static double ClosenessOf<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var distances = graph.ShortestPathsFrom(vertex, weightSelector).Distances;
        var reachable = distances.Count; // includes the vertex itself at distance zero
        var total = distances.Values.Sum(double.CreateChecked);
        return reachable > 1 && total > 0
            ? (reachable - 1.0) / (graph.VertexCount - 1.0) * ((reachable - 1.0) / total)
            : 0.0;
    }

    /// <summary>Gets hop-count betweenness centrality (Brandes, breadth-first).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.BetweennessCentrality(CancellationToken.None);

    /// <summary>Gets hop-count betweenness centrality, observing <paramref name="cancellationToken"/> between source vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>Raw betweenness per vertex.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        return BrandesAccumulate(graph, source => BreadthFirstStage(graph, source), cancellationToken);
    }

    /// <summary>
    /// Gets hop-count betweenness centrality with the per-source Brandes
    /// passes running in parallel. The token inside
    /// <paramref name="parallelOptions"/> cancels cooperatively between
    /// sources.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure; only read, never mutated.</param>
    /// <param name="parallelOptions">Degree of parallelism and cancellation.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    /// <exception cref="OperationCanceledException">The token inside the options was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        ParallelOptions parallelOptions)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(parallelOptions);
        parallelOptions.CancellationToken.ThrowIfCancellationRequested();
        return BrandesAccumulateParallel(graph, source => BreadthFirstStage(graph, source), parallelOptions);
    }

    /// <summary>Gets weighted betweenness centrality (Brandes over Dijkstra).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.BetweennessCentrality(weightSelector, CancellationToken.None);

    /// <summary>Gets weighted betweenness centrality, observing <paramref name="cancellationToken"/> between source vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>Raw betweenness per vertex.</returns>
    /// <exception cref="NegativeWeightException">A negative edge weight was encountered.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        cancellationToken.ThrowIfCancellationRequested();
        return BrandesAccumulate(graph, source => DijkstraStage(graph, weightSelector, source), cancellationToken);
    }

    /// <summary>
    /// Gets weighted betweenness centrality with the per-source Brandes
    /// passes running in parallel. <paramref name="weightSelector"/> is
    /// invoked concurrently and must be pure. The token inside
    /// <paramref name="parallelOptions"/> cancels cooperatively between
    /// sources.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The graph to measure; only read, never mutated.</param>
    /// <param name="weightSelector">Maps an edge to its weight; must be safe to call concurrently.</param>
    /// <param name="parallelOptions">Degree of parallelism and cancellation.</param>
    /// <returns>Raw betweenness per vertex (undirected pair contributions counted once).</returns>
    /// <exception cref="AggregateException">A per-source pass failed, e.g. with <see cref="NegativeWeightException"/>.</exception>
    /// <exception cref="OperationCanceledException">The token inside the options was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> BetweennessCentrality<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        ParallelOptions parallelOptions)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        ArgumentNullException.ThrowIfNull(parallelOptions);
        parallelOptions.CancellationToken.ThrowIfCancellationRequested();
        return BrandesAccumulateParallel(graph, source => DijkstraStage(graph, weightSelector, source), parallelOptions);
    }

    private static (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)
        BreadthFirstStage<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph, TVertex source)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, int>(comparer) { [source] = 0 };
        var sigma = new Dictionary<TVertex, double>(comparer) { [source] = 1.0 };
        var predecessors = new Dictionary<TVertex, List<TVertex>>(comparer) { [source] = [] };
        var order = new List<TVertex>();
        var queue = new Queue<TVertex>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);
            foreach (var (neighbor, _) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                if (!distance.TryGetValue(neighbor, out var known))
                {
                    distance[neighbor] = distance[current] + 1;
                    sigma[neighbor] = 0.0;
                    predecessors[neighbor] = [];
                    queue.Enqueue(neighbor);
                    known = distance[neighbor];
                }

                if (known == distance[current] + 1)
                {
                    sigma[neighbor] += sigma[current];
                    predecessors[neighbor].Add(current);
                }
            }
        }

        return (order, sigma, predecessors);
    }

    private static (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)
        DijkstraStage<TVertex, TEdge, TWeight>(
            IReadOnlyGraph<TVertex, TEdge> graph,
            Func<TEdge, TWeight> weightSelector,
            TVertex source)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var comparer = graph.VertexComparer;
        var distance = new Dictionary<TVertex, TWeight>(comparer) { [source] = TWeight.Zero };
        var sigma = new Dictionary<TVertex, double>(comparer) { [source] = 1.0 };
        var predecessors = new Dictionary<TVertex, List<TVertex>>(comparer) { [source] = [] };
        var order = new List<TVertex>();
        var settled = new HashSet<TVertex>(comparer);
        var frontier = new PriorityQueue<TVertex, TWeight>();
        frontier.Enqueue(source, TWeight.Zero);

        while (frontier.TryDequeue(out var current, out _))
        {
            if (!settled.Add(current))
            {
                continue;
            }

            order.Add(current);
            foreach (var (neighbor, edge) in GraphTraversalCore.OutgoingArcs(graph, current))
            {
                var weight = weightSelector(edge);
                if (weight < TWeight.Zero)
                {
                    throw new NegativeWeightException(
                        $"Edge '{edge}' has negative weight {weight}; betweenness centrality requires non-negative weights.");
                }

                if (settled.Contains(neighbor))
                {
                    continue;
                }

                var candidate = distance[current] + weight;
                if (!distance.TryGetValue(neighbor, out var known) || candidate < known)
                {
                    distance[neighbor] = candidate;
                    sigma[neighbor] = sigma[current];
                    predecessors[neighbor] = [current];
                    frontier.Enqueue(neighbor, candidate);
                }
                else if (candidate == known)
                {
                    sigma[neighbor] += sigma[current];
                    predecessors[neighbor].Add(current);
                }
            }
        }

        return (order, sigma, predecessors);
    }

    /// <summary>
    /// Computes PageRank by power iteration with uniform teleportation;
    /// dangling-vertex mass is redistributed uniformly, so ranks always sum
    /// to one. Parallel edges each carry their share of the source's rank.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to rank.</param>
    /// <param name="damping">The damping factor in [0, 1]; 0.85 is the classic choice.</param>
    /// <param name="maxIterations">The iteration cap, at least 1.</param>
    /// <param name="tolerance">The L1 convergence threshold.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively between power iterations.</param>
    /// <returns>PageRank per vertex, summing to 1 (empty for the empty graph).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Damping leaves [0, 1] or <paramref name="maxIterations"/> is below 1.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> PageRank<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        double damping = 0.85,
        int maxIterations = 100,
        double tolerance = 1e-9,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegative(damping);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(damping, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);

        var count = graph.VertexCount;
        var ranks = new Dictionary<TVertex, double>(graph.VertexComparer);
        if (count == 0)
        {
            return ranks;
        }

        foreach (var vertex in graph.Vertices)
        {
            ranks[vertex] = 1.0 / count;
        }

        // Out-degrees are loop-invariant; cache them once instead of asking
        // the graph per in-edge per iteration.
        var outDegree = new Dictionary<TVertex, int>(count, graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            outDegree[vertex] = graph.OutDegree(vertex);
        }

        var next = new Dictionary<TVertex, double>(count, graph.VertexComparer);
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var danglingMass = 0.0;
            foreach (var (vertex, degree) in outDegree)
            {
                if (degree == 0)
                {
                    danglingMass += ranks[vertex];
                }
            }

            var change = 0.0;
            foreach (var vertex in graph.Vertices)
            {
                var incoming = 0.0;
                foreach (var edge in graph.InEdges(vertex))
                {
                    incoming += ranks[edge.Source] / outDegree[edge.Source];
                }

                var rank = ((1.0 - damping) / count) + (damping * (incoming + (danglingMass / count)));
                next[vertex] = rank;
                change += Math.Abs(rank - ranks[vertex]);
            }

            // Swap the two buffers instead of allocating a dictionary per iteration.
            (ranks, next) = (next, ranks);
            if (change < tolerance)
            {
                break;
            }
        }

        return ranks;
    }

    /// <summary>
    /// Computes eigenvector centrality by shifted power iteration
    /// (x ← (A + I)·x, so bipartite graphs cannot oscillate): each vertex's
    /// score is proportional to the sum of its in-neighbors' scores
    /// (neighbors, when undirected), L2-normalized. On DAGs the spectrum is
    /// degenerate and scores drift toward the sinks without converging —
    /// use <see cref="KatzCentrality{TVertex, TEdge}"/> there.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="maxIterations">The iteration cap, at least 1.</param>
    /// <param name="tolerance">The L1 convergence threshold.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively between power iterations.</param>
    /// <returns>Eigenvector centrality per vertex, L2-normalized (empty for the empty graph).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxIterations"/> is below 1.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> EigenvectorCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        int maxIterations = 100,
        double tolerance = 1e-9,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);

        var count = graph.VertexCount;
        var scores = new Dictionary<TVertex, double>(count, graph.VertexComparer);
        if (count == 0)
        {
            return scores;
        }

        foreach (var vertex in graph.Vertices)
        {
            scores[vertex] = 1.0 / count;
        }

        var next = new Dictionary<TVertex, double>(count, graph.VertexComparer);
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The identity shift keeps every entry positive, so the norm
            // below can never vanish.
            foreach (var vertex in graph.Vertices)
            {
                next[vertex] = scores[vertex];
            }

            PushScores(graph, scores, next);

            var norm = Math.Sqrt(next.Values.Sum(score => score * score));
            var change = 0.0;
            foreach (var vertex in graph.Vertices)
            {
                var normalized = next[vertex] / norm;
                next[vertex] = normalized;
                change += Math.Abs(normalized - scores[vertex]);
            }

            (scores, next) = (next, scores);
            if (change < tolerance)
            {
                break;
            }
        }

        return scores;
    }

    /// <summary>
    /// Computes Katz centrality by fixed-point iteration: every vertex gets a
    /// base score <paramref name="beta"/> plus <paramref name="alpha"/> times
    /// its in-neighbors' scores (neighbors, when undirected), L2-normalized.
    /// Unlike eigenvector centrality it stays meaningful on DAGs.
    /// Convergence requires <paramref name="alpha"/> below the reciprocal of
    /// the spectral radius; past the iteration cap the current values are
    /// returned as-is.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to measure.</param>
    /// <param name="alpha">The attenuation factor, above 0; 0.1 is the classic choice.</param>
    /// <param name="beta">The base score every vertex starts from.</param>
    /// <param name="maxIterations">The iteration cap, at least 1.</param>
    /// <param name="tolerance">The L1 convergence threshold.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively between iterations.</param>
    /// <returns>Katz centrality per vertex, L2-normalized (empty for the empty graph).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="alpha"/> is not positive or <paramref name="maxIterations"/> is below 1.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static IReadOnlyDictionary<TVertex, double> KatzCentrality<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        double alpha = 0.1,
        double beta = 1.0,
        int maxIterations = 100,
        double tolerance = 1e-9,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alpha);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);

        var count = graph.VertexCount;
        var scores = new Dictionary<TVertex, double>(count, graph.VertexComparer);
        if (count == 0)
        {
            return scores;
        }

        foreach (var vertex in graph.Vertices)
        {
            scores[vertex] = 0.0;
        }

        var next = new Dictionary<TVertex, double>(count, graph.VertexComparer);
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var vertex in graph.Vertices)
            {
                next[vertex] = 0.0;
            }

            PushScores(graph, scores, next);

            var change = 0.0;
            foreach (var vertex in graph.Vertices)
            {
                var score = beta + (alpha * next[vertex]);
                next[vertex] = score;
                change += Math.Abs(score - scores[vertex]);
            }

            (scores, next) = (next, scores);
            if (change < tolerance)
            {
                break;
            }
        }

        var norm = Math.Sqrt(scores.Values.Sum(score => score * score));
        if (norm > 0.0)
        {
            foreach (var vertex in scores.Keys.ToList())
            {
                scores[vertex] /= norm;
            }
        }

        return scores;
    }

    /// <summary>
    /// One matrix-vector step, push form: every vertex sends its score along
    /// its outgoing arcs (all incident arcs, when undirected), so the target
    /// buffer receives each vertex's in-neighbor sum. Parallel edges push
    /// once per instance.
    /// </summary>
    private static void PushScores<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Dictionary<TVertex, double> scores,
        Dictionary<TVertex, double> next)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        foreach (var vertex in graph.Vertices)
        {
            var score = scores[vertex];
            foreach (var (neighbor, _) in GraphTraversalCore.OutgoingArcs(graph, vertex))
            {
                next[neighbor] += score;
            }
        }
    }

    /// <summary>
    /// The shared Brandes dependency-accumulation phase: walk vertices in
    /// reverse settlement order, pushing each vertex's dependency onto its
    /// shortest-path predecessors. Undirected results are halved because every
    /// unordered pair is visited from both endpoints.
    /// </summary>
    private static Dictionary<TVertex, double> BrandesAccumulate<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TVertex, (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)> stage,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var centrality = new Dictionary<TVertex, double>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            centrality[vertex] = 0.0;
        }

        foreach (var source in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AccumulateDependencies(source, stage(source), comparer, centrality);
        }

        if (!graph.IsDirected)
        {
            HalveAll(centrality);
        }

        return centrality;
    }

    /// <summary>
    /// The parallel counterpart of <see cref="BrandesAccumulate{TVertex, TEdge}"/>:
    /// per-source passes run concurrently, each thread accumulating into its
    /// own partial-score dictionary, merged under a lock at the end. Merge
    /// order is nondeterministic, so results can differ from the sequential
    /// reference by floating-point rounding only.
    /// </summary>
    private static Dictionary<TVertex, double> BrandesAccumulateParallel<TVertex, TEdge>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TVertex, (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors)> stage,
        ParallelOptions parallelOptions)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var centrality = new Dictionary<TVertex, double>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            centrality[vertex] = 0.0;
        }

        var gate = new object();
        Parallel.ForEach(
            graph.Vertices.ToList(),
            parallelOptions,
            () => new Dictionary<TVertex, double>(comparer),
            (source, _, partial) =>
            {
                AccumulateDependencies(source, stage(source), comparer, partial);
                return partial;
            },
            partial =>
            {
                lock (gate)
                {
                    foreach (var (vertex, score) in partial)
                    {
                        centrality[vertex] += score;
                    }
                }
            });

        if (!graph.IsDirected)
        {
            HalveAll(centrality);
        }

        return centrality;
    }

    /// <summary>
    /// One source's Brandes dependency-accumulation phase: walk vertices in
    /// reverse settlement order, pushing each vertex's dependency onto its
    /// shortest-path predecessors, adding the result into
    /// <paramref name="centrality"/>.
    /// </summary>
    private static void AccumulateDependencies<TVertex>(
        TVertex source,
        (List<TVertex> Order, Dictionary<TVertex, double> Sigma, Dictionary<TVertex, List<TVertex>> Predecessors) stage,
        IEqualityComparer<TVertex> comparer,
        Dictionary<TVertex, double> centrality)
        where TVertex : notnull
    {
        var (order, sigma, predecessors) = stage;
        var dependency = new Dictionary<TVertex, double>(comparer);
        foreach (var vertex in order)
        {
            dependency[vertex] = 0.0;
        }

        for (var i = order.Count - 1; i >= 0; i--)
        {
            var vertex = order[i];
            foreach (var predecessor in predecessors[vertex])
            {
                dependency[predecessor] += sigma[predecessor] / sigma[vertex] * (1.0 + dependency[vertex]);
            }

            if (!comparer.Equals(vertex, source))
            {
                centrality[vertex] = centrality.GetValueOrDefault(vertex) + dependency[vertex];
            }
        }
    }

    /// <summary>Halves every score: undirected accumulation visits each unordered pair from both endpoints.</summary>
    private static void HalveAll<TVertex>(Dictionary<TVertex, double> centrality)
        where TVertex : notnull
    {
        foreach (var vertex in centrality.Keys.ToList())
        {
            centrality[vertex] /= 2.0;
        }
    }
}
