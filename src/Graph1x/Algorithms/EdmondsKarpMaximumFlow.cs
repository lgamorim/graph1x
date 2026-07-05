using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// The Edmonds-Karp maximum-flow algorithm: Ford-Fulkerson with breadth-first
/// augmenting paths, giving O(V·E²) independently of capacity values. With
/// floating-point capacities, tiny rounding residues are possible — integer
/// or decimal capacities are exact. For large or dense networks consider
/// <see cref="DinicMaximumFlow{TVertex, TEdge, TWeight}"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity type.</typeparam>
public sealed class EdmondsKarpMaximumFlow<TVertex, TEdge, TWeight> : IMaximumFlowAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _capacitySelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's capacity.</summary>
    /// <param name="capacitySelector">Maps an edge to its capacity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="capacitySelector"/> is <see langword="null"/>.</exception>
    public EdmondsKarpMaximumFlow(Func<TEdge, TWeight> capacitySelector)
    {
        ArgumentNullException.ThrowIfNull(capacitySelector);
        _capacitySelector = capacitySelector;
    }

    /// <inheritdoc/>
    public MaximumFlowResult<TVertex, TEdge, TWeight> FindMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink)
        => FindMaximumFlow(graph, source, sink, CancellationToken.None);

    /// <inheritdoc/>
    /// <remarks>Cancellation is observed between augmenting paths.</remarks>
    public MaximumFlowResult<TVertex, TEdge, TWeight> FindMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        CancellationToken cancellationToken)
    {
        FlowGuards.Validate(graph, source, sink);
        cancellationToken.ThrowIfCancellationRequested();

        var network = new ResidualNetwork<TVertex, TEdge, TWeight>(graph, _capacitySelector);
        cancellationToken.ThrowIfCancellationRequested();
        var sourceIndex = network.IndexOf(source);
        var sinkIndex = network.IndexOf(sink);

        var total = TWeight.Zero;
        while (network.FindAugmentingPath(sourceIndex, sinkIndex) is { } parentArc)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += network.Augment(parentArc, sourceIndex, sinkIndex);
        }

        return network.BuildResult(source, sink, total);
    }
}
