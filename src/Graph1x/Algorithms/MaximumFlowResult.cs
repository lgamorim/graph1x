using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// The outcome of a maximum-flow computation: the flow value, the flow carried
/// by every edge, and a minimum source/sink cut certifying optimality
/// (its capacity equals the flow value).
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity type.</typeparam>
public sealed class MaximumFlowResult<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    internal MaximumFlowResult(
        TVertex source,
        TVertex sink,
        TWeight flowValue,
        IReadOnlyList<(TEdge Edge, TWeight Flow)> edgeFlows,
        IReadOnlySet<TVertex> sourceSideOfMinCut,
        IReadOnlyList<TEdge> minCutEdges)
    {
        Source = source;
        Sink = sink;
        FlowValue = flowValue;
        EdgeFlows = edgeFlows;
        SourceSideOfMinCut = sourceSideOfMinCut;
        MinCutEdges = minCutEdges;
    }

    /// <summary>Gets the vertex the flow originates from.</summary>
    public TVertex Source { get; }

    /// <summary>Gets the vertex the flow drains into.</summary>
    public TVertex Sink { get; }

    /// <summary>Gets the value of the maximum flow.</summary>
    public TWeight FlowValue { get; }

    /// <summary>
    /// Gets the flow carried by each edge of the network (parallel edges are
    /// listed individually). Edges carrying zero flow are included.
    /// </summary>
    public IReadOnlyList<(TEdge Edge, TWeight Flow)> EdgeFlows { get; }

    /// <summary>
    /// Gets the source side of a minimum cut: the vertices still reachable
    /// from <see cref="Source"/> in the residual network.
    /// </summary>
    public IReadOnlySet<TVertex> SourceSideOfMinCut { get; }

    /// <summary>
    /// Gets the edges of the minimum cut: every edge leaving
    /// <see cref="SourceSideOfMinCut"/>. Their total capacity equals
    /// <see cref="FlowValue"/>.
    /// </summary>
    public IReadOnlyList<TEdge> MinCutEdges { get; }
}
