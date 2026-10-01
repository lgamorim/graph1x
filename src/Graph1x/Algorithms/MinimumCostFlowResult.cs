using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// The outcome of a minimum-cost maximum-flow computation: the maximum flow
/// value, the smallest total cost any flow of that value can achieve, and the
/// flow carried by every edge.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity and cost type.</typeparam>
public sealed class MinimumCostFlowResult<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    internal MinimumCostFlowResult(
        TVertex source,
        TVertex sink,
        TWeight flowValue,
        TWeight totalCost,
        IReadOnlyList<(TEdge Edge, TWeight Flow)> edgeFlows)
    {
        Source = source;
        Sink = sink;
        FlowValue = flowValue;
        TotalCost = totalCost;
        EdgeFlows = edgeFlows;
    }

    /// <summary>Gets the vertex the flow originates from.</summary>
    public TVertex Source { get; }

    /// <summary>Gets the vertex the flow drains into.</summary>
    public TVertex Sink { get; }

    /// <summary>Gets the value of the maximum flow.</summary>
    public TWeight FlowValue { get; }

    /// <summary>
    /// Gets the total cost of the flow: the sum over all edges of the edge's
    /// flow times its cost — the minimum achievable for any flow of
    /// <see cref="FlowValue"/>.
    /// </summary>
    public TWeight TotalCost { get; }

    /// <summary>
    /// Gets the flow carried by each edge of the network (parallel edges are
    /// listed individually). Edges carrying zero flow are included.
    /// </summary>
    public IReadOnlyList<(TEdge Edge, TWeight Flow)> EdgeFlows { get; }
}
