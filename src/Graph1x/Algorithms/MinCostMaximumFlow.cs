using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Minimum-cost maximum-flow via successive shortest augmenting paths with
/// vertex potentials: Bellman-Ford establishes initial potentials (so negative
/// edge costs are supported), then every augmentation runs Dijkstra on reduced
/// costs. The result carries the maximum flow at the smallest possible total
/// cost. As a special case this solves the assignment problem on unit
/// capacities.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity and cost type.</typeparam>
public sealed class MinCostMaximumFlow<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _capacitySelector;
    private readonly Func<TEdge, TWeight> _costSelector;

    /// <summary>Initializes the algorithm with the functions that read an edge's capacity and cost.</summary>
    /// <param name="capacitySelector">Maps an edge to its (non-negative) capacity.</param>
    /// <param name="costSelector">Maps an edge to its per-unit cost; negative costs are allowed as long as no negative-cost cycle is reachable from the source.</param>
    /// <exception cref="ArgumentNullException">A selector is <see langword="null"/>.</exception>
    public MinCostMaximumFlow(Func<TEdge, TWeight> capacitySelector, Func<TEdge, TWeight> costSelector)
    {
        ArgumentNullException.ThrowIfNull(capacitySelector);
        ArgumentNullException.ThrowIfNull(costSelector);
        _capacitySelector = capacitySelector;
        _costSelector = costSelector;
    }

    /// <summary>
    /// Computes the maximum flow from <paramref name="source"/> to
    /// <paramref name="sink"/> at minimum total cost.
    /// </summary>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <returns>The flow value, its total cost, and per-edge flows.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    /// <exception cref="NegativeCycleException">A negative-cost cycle is reachable from the source.</exception>
    public MinimumCostFlowResult<TVertex, TEdge, TWeight> FindMinimumCostMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink)
        => FindMinimumCostMaximumFlow(graph, source, sink, CancellationToken.None);

    /// <summary>
    /// Computes the maximum flow from <paramref name="source"/> to
    /// <paramref name="sink"/> at minimum total cost, observing
    /// <paramref name="cancellationToken"/> between augmenting paths.
    /// </summary>
    /// <param name="graph">The directed flow network.</param>
    /// <param name="source">The vertex the flow originates from.</param>
    /// <param name="sink">The vertex the flow drains into.</param>
    /// <param name="cancellationToken">Cancels the computation cooperatively.</param>
    /// <returns>The flow value, its total cost, and per-edge flows.</returns>
    /// <exception cref="NegativeWeightException">An edge has negative capacity.</exception>
    /// <exception cref="NegativeCycleException">A negative-cost cycle is reachable from the source.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public MinimumCostFlowResult<TVertex, TEdge, TWeight> FindMinimumCostMaximumFlow(
        IDirectedGraph<TVertex, TEdge> graph,
        TVertex source,
        TVertex sink,
        CancellationToken cancellationToken)
    {
        FlowGuards.Validate(graph, source, sink);
        cancellationToken.ThrowIfCancellationRequested();

        var network = new ResidualNetwork<TVertex, TEdge, TWeight>(graph, _capacitySelector, _costSelector);
        var vertexCount = network.VertexCount;
        var sourceIndex = network.IndexOf(source);
        var sinkIndex = network.IndexOf(sink);

        var (potential, hasPotential) = InitialPotentials(network, sourceIndex);
        cancellationToken.ThrowIfCancellationRequested();

        var total = TWeight.Zero;
        var distance = new TWeight[vertexCount];
        var reached = new bool[vertexCount];
        var settled = new bool[vertexCount];
        var parentArc = new int[vertexCount];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(reached);
            Array.Clear(settled);
            Array.Fill(parentArc, -1);
            var queue = new PriorityQueue<int, TWeight>();
            reached[sourceIndex] = true;
            distance[sourceIndex] = TWeight.Zero;
            queue.Enqueue(sourceIndex, TWeight.Zero);

            while (queue.TryDequeue(out var current, out _))
            {
                if (settled[current])
                {
                    continue;
                }

                settled[current] = true;
                foreach (var arc in network.IncidentArcs(current))
                {
                    if (network.Residual(arc) <= TWeight.Zero)
                    {
                        continue;
                    }

                    var head = network.Head(arc);
                    if (settled[head] || !hasPotential[head])
                    {
                        continue;
                    }

                    var candidate = distance[current] + network.Cost(arc) + potential[current] - potential[head];
                    if (!reached[head] || candidate < distance[head])
                    {
                        reached[head] = true;
                        distance[head] = candidate;
                        parentArc[head] = arc;
                        queue.Enqueue(head, candidate);
                    }
                }
            }

            if (!settled[sinkIndex])
            {
                break; // sink unreachable in the residual network: flow is maximum
            }

            for (var vertex = 0; vertex < vertexCount; vertex++)
            {
                if (settled[vertex])
                {
                    potential[vertex] += distance[vertex];
                }
            }

            total += network.Augment(parentArc, sourceIndex, sinkIndex);
        }

        return new MinimumCostFlowResult<TVertex, TEdge, TWeight>(
            source, sink, total, network.TotalCost(), network.EdgeFlows());
    }

    /// <summary>
    /// Bellman-Ford from the source over the initial residual arcs, producing
    /// the potentials that make all reduced costs non-negative. Vertices
    /// unreachable from the source get no potential; they can never lie on an
    /// augmenting path, so later Dijkstra runs skip them.
    /// </summary>
    private static (TWeight[] Potential, bool[] HasPotential) InitialPotentials(
        ResidualNetwork<TVertex, TEdge, TWeight> network, int sourceIndex)
    {
        var vertexCount = network.VertexCount;
        var potential = new TWeight[vertexCount];
        var hasPotential = new bool[vertexCount];
        hasPotential[sourceIndex] = true;

        for (var pass = 0; pass < vertexCount; pass++)
        {
            var relaxed = false;
            for (var tail = 0; tail < vertexCount; tail++)
            {
                if (!hasPotential[tail])
                {
                    continue;
                }

                foreach (var arc in network.IncidentArcs(tail))
                {
                    if (network.Residual(arc) <= TWeight.Zero)
                    {
                        continue;
                    }

                    var head = network.Head(arc);
                    var candidate = potential[tail] + network.Cost(arc);
                    if (!hasPotential[head] || candidate < potential[head])
                    {
                        if (pass == vertexCount - 1)
                        {
                            throw new NegativeCycleException(
                                "The network contains a negative-cost cycle reachable from the source; minimum-cost flow is undefined.");
                        }

                        hasPotential[head] = true;
                        potential[head] = candidate;
                        relaxed = true;
                    }
                }
            }

            if (!relaxed)
            {
                break;
            }
        }

        return (potential, hasPotential);
    }
}
