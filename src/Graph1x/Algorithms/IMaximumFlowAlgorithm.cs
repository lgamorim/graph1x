using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// A maximum-flow strategy for directed graphs with non-negative edge
/// capacities. Implementations compute the maximum flow from a source to a
/// sink together with a certifying minimum cut.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity type.</typeparam>
public interface IMaximumFlowAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    /// <summary>Computes the maximum flow from <paramref name="source"/> to <paramref name="sink"/>.</summary>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <returns>The flow value, per-edge flows, and a minimum cut.</returns>
    /// <exception cref="ArgumentException">Either endpoint is missing, or source equals sink.</exception>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    MaximumFlowResult<TVertex, TEdge, TWeight> FindMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink);
}
