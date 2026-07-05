using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Convenience entry points for maximum-flow queries, defaulting to
/// Edmonds-Karp.
/// </summary>
public static class GraphMaximumFlowExtensions
{
    /// <summary>
    /// Computes the maximum flow from <paramref name="source"/> to
    /// <paramref name="sink"/> using Edmonds-Karp and
    /// <paramref name="capacitySelector"/> to read edge capacities.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric capacity type.</typeparam>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <param name="capacitySelector">Maps an edge to its capacity.</param>
    /// <returns>The flow value, per-edge flows, and a minimum cut.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    public static MaximumFlowResult<TVertex, TEdge, TWeight> MaximumFlow<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        Func<TEdge, TWeight> capacitySelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new EdmondsKarpMaximumFlow<TVertex, TEdge, TWeight>(capacitySelector)
            .FindMaximumFlow(graph, source, sink);

    /// <summary>
    /// Computes the maximum flow from <paramref name="source"/> to
    /// <paramref name="sink"/> using Edmonds-Karp and the capacities carried
    /// by the graph's <see cref="WeightedEdge{TVertex, TWeight}"/> edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric capacity type.</typeparam>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <returns>The flow value, per-edge flows, and a minimum cut.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    public static MaximumFlowResult<TVertex, WeightedEdge<TVertex, TWeight>, TWeight> MaximumFlow<TVertex, TWeight>(
        this IDirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>> graph,
        TVertex source,
        TVertex sink)
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => graph.MaximumFlow(source, sink, edge => edge.Weight);

    /// <summary>
    /// Computes the maximum flow using Edmonds-Karp, observing
    /// <paramref name="cancellationToken"/> between augmenting paths.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric capacity type.</typeparam>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <param name="capacitySelector">Maps an edge to its capacity.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>The flow value, per-edge flows, and a minimum cut.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static MaximumFlowResult<TVertex, TEdge, TWeight> MaximumFlow<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        Func<TEdge, TWeight> capacitySelector,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new EdmondsKarpMaximumFlow<TVertex, TEdge, TWeight>(capacitySelector)
            .FindMaximumFlow(graph, source, sink, cancellationToken);
}
