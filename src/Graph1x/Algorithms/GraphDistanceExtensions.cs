using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Distance metrics: eccentricity, diameter, radius, center, periphery, and
/// average path length, computed from Dijkstra single-source runs. The graph
/// must be connected (strongly connected when directed) — infinite distances
/// are rejected up front instead of being encoded as sentinel values.
/// Weighted overloads take a selector; the default counts hops.
/// </summary>
public static class GraphDistanceExtensions
{
    /// <summary>Gets the greatest shortest-path distance from <paramref name="vertex"/> to any other vertex.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="vertex">The vertex whose eccentricity to compute.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The eccentricity.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static TWeight Eccentricity<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        GraphTraversalCore.ValidateEndpoint(graph, vertex, nameof(vertex));
        ValidateConnected(graph);
        return EccentricityOf(graph, vertex, weightSelector);
    }

    /// <summary>Gets the hop-count eccentricity of <paramref name="vertex"/>.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="vertex">The vertex whose eccentricity to compute.</param>
    /// <returns>The eccentricity in hops.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static int Eccentricity<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph, TVertex vertex)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.Eccentricity(vertex, _ => 1);

    /// <summary>Gets the greatest eccentricity: the longest shortest path in the graph.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The diameter.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static TWeight Diameter<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => Eccentricities(graph, weightSelector).Values.Max()!;

    /// <summary>Gets the diameter, observing <paramref name="cancellationToken"/> between vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>The diameter.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static TWeight Diameter<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => Eccentricities(graph, weightSelector, cancellationToken).Values.Max()!;

    /// <summary>Gets the hop-count diameter.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <returns>The diameter in hops.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static int Diameter<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.Diameter(_ => 1);

    /// <summary>Gets the smallest eccentricity.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The radius.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static TWeight Radius<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => Eccentricities(graph, weightSelector).Values.Min()!;

    /// <summary>Gets the hop-count radius.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <returns>The radius in hops.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static int Radius<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.Radius(_ => 1);

    /// <summary>Gets the center: the vertices whose eccentricity equals the radius.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The central vertices.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static IReadOnlySet<TVertex> Center<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => SelectByEccentricity(graph, weightSelector, pickMaximum: false);

    /// <summary>Gets the hop-count center.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <returns>The central vertices.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static IReadOnlySet<TVertex> Center<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.Center(_ => 1);

    /// <summary>Gets the periphery: the vertices whose eccentricity equals the diameter.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The peripheral vertices.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static IReadOnlySet<TVertex> Periphery<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => SelectByEccentricity(graph, weightSelector, pickMaximum: true);

    /// <summary>Gets the hop-count periphery.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <returns>The peripheral vertices.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static IReadOnlySet<TVertex> Periphery<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.Periphery(_ => 1);

    /// <summary>
    /// Gets the mean shortest-path distance over all ordered vertex pairs.
    /// A single-vertex graph has no pairs and yields zero.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <returns>The average path length.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static double AveragePathLength<TVertex, TEdge, TWeight>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.AveragePathLength(weightSelector, CancellationToken.None);

    /// <summary>Gets the mean shortest-path distance, observing <paramref name="cancellationToken"/> between vertices.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <param name="weightSelector">Maps an edge to its weight.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>The average path length.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static double AveragePathLength<TVertex, TEdge, TWeight>(
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
        ValidateConnected(graph);

        if (graph.VertexCount < 2)
        {
            return 0.0;
        }

        var total = 0.0;
        foreach (var source in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var paths = graph.ShortestPathsFrom(source, weightSelector);
            foreach (var (target, distance) in paths.Distances)
            {
                if (!graph.VertexComparer.Equals(target, source))
                {
                    total += double.CreateChecked(distance);
                }
            }
        }

        return total / ((double)graph.VertexCount * (graph.VertexCount - 1));
    }

    /// <summary>Gets the mean hop count over all ordered vertex pairs.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The connected graph to measure.</param>
    /// <returns>The average path length in hops.</returns>
    /// <exception cref="InvalidOperationException">The graph is empty or not (strongly) connected.</exception>
    public static double AveragePathLength<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.AveragePathLength(_ => 1);

    private static Dictionary<TVertex, TWeight> Eccentricities<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        CancellationToken cancellationToken = default)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(weightSelector);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateConnected(graph);

        var eccentricities = new Dictionary<TVertex, TWeight>(graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            eccentricities[vertex] = EccentricityOf(graph, vertex, weightSelector);
        }

        return eccentricities;
    }

    private static TWeight EccentricityOf<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        TVertex vertex,
        Func<TEdge, TWeight> weightSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => graph.ShortestPathsFrom(vertex, weightSelector).Distances.Values.Max()!;

    private static IReadOnlySet<TVertex> SelectByEccentricity<TVertex, TEdge, TWeight>(
        IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TEdge, TWeight> weightSelector,
        bool pickMaximum)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
    {
        var eccentricities = Eccentricities(graph, weightSelector);
        var bound = pickMaximum ? eccentricities.Values.Max() : eccentricities.Values.Min();
        var selected = new HashSet<TVertex>(graph.VertexComparer);
        foreach (var (vertex, eccentricity) in eccentricities)
        {
            if (eccentricity == bound)
            {
                selected.Add(vertex);
            }
        }

        return selected;
    }

    private static void ValidateConnected<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        if (graph.VertexCount == 0)
        {
            throw new InvalidOperationException("Distance metrics are undefined for the empty graph.");
        }

        if (graph is IDirectedGraph<TVertex, TEdge> directed)
        {
            if (directed.StronglyConnectedComponents().Count != 1)
            {
                throw new InvalidOperationException(
                    "Distance metrics require a strongly connected directed graph; some distances would be infinite.");
            }

            return;
        }

        if (graph.ConnectedComponents().Count != 1)
        {
            throw new InvalidOperationException(
                "Distance metrics require a connected graph; some distances would be infinite.");
        }
    }
}
