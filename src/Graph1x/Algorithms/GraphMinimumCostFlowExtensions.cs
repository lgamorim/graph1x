using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Convenience entry points for minimum-cost maximum-flow queries.
/// </summary>
public static class GraphMinimumCostFlowExtensions
{
    /// <summary>
    /// Computes the maximum flow from <paramref name="source"/> to
    /// <paramref name="sink"/> at minimum total cost, using
    /// <paramref name="capacitySelector"/> and <paramref name="costSelector"/>
    /// to read each edge's capacity and per-unit cost.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric capacity and cost type.</typeparam>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <param name="capacitySelector">Maps an edge to its (non-negative) capacity.</param>
    /// <param name="costSelector">Maps an edge to its per-unit cost.</param>
    /// <returns>The flow value, its total cost, and per-edge flows.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    /// <exception cref="NegativeCycleException">A negative-cost cycle is reachable from the source.</exception>
    public static MinimumCostFlowResult<TVertex, TEdge, TWeight> MinimumCostMaximumFlow<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        Func<TEdge, TWeight> capacitySelector,
        Func<TEdge, TWeight> costSelector)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new MinCostMaximumFlow<TVertex, TEdge, TWeight>(capacitySelector, costSelector)
            .FindMinimumCostMaximumFlow(graph, source, sink);

    /// <summary>
    /// Computes the maximum flow at minimum total cost, observing
    /// <paramref name="cancellationToken"/> between augmenting paths.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <typeparam name="TWeight">The numeric capacity and cost type.</typeparam>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <param name="capacitySelector">Maps an edge to its (non-negative) capacity.</param>
    /// <param name="costSelector">Maps an edge to its per-unit cost.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>The flow value, its total cost, and per-edge flows.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    /// <exception cref="NegativeCycleException">A negative-cost cycle is reachable from the source.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static MinimumCostFlowResult<TVertex, TEdge, TWeight> MinimumCostMaximumFlow<TVertex, TEdge, TWeight>(
        this IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        Func<TEdge, TWeight> capacitySelector,
        Func<TEdge, TWeight> costSelector,
        CancellationToken cancellationToken)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        where TWeight : INumber<TWeight>
        => new MinCostMaximumFlow<TVertex, TEdge, TWeight>(capacitySelector, costSelector)
            .FindMinimumCostMaximumFlow(graph, source, sink, cancellationToken);
}
