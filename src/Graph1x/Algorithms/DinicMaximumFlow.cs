using System.Numerics;
using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Dinic's maximum-flow algorithm: repeated BFS level graphs, each saturated
/// by a blocking flow found with cursor-guided depth-first walks. O(V²·E) in
/// general and substantially faster than Edmonds-Karp on large or dense
/// networks (O(E·√V) on unit-capacity graphs); interchangeable with it behind
/// <see cref="IMaximumFlowAlgorithm{TVertex, TEdge, TWeight}"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
/// <typeparam name="TWeight">The numeric capacity type.</typeparam>
public sealed class DinicMaximumFlow<TVertex, TEdge, TWeight> : IMaximumFlowAlgorithm<TVertex, TEdge, TWeight>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
    where TWeight : INumber<TWeight>
{
    private readonly Func<TEdge, TWeight> _capacitySelector;

    /// <summary>Initializes the algorithm with the function that reads an edge's capacity.</summary>
    /// <param name="capacitySelector">Maps an edge to its capacity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="capacitySelector"/> is <see langword="null"/>.</exception>
    public DinicMaximumFlow(Func<TEdge, TWeight> capacitySelector)
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
    /// <remarks>Cancellation is observed between level-graph phases.</remarks>
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
        while (BuildLevels(network, sourceIndex) is { } levels && levels[sinkIndex] >= 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += BlockingFlow(network, levels, sourceIndex, sinkIndex);
        }

        return network.BuildResult(source, sink, total);
    }

    /// <summary>BFS phase: distance labels over positive-residual arcs (-1 = unreachable).</summary>
    private static int[] BuildLevels(ResidualNetwork<TVertex, TEdge, TWeight> network, int source)
    {
        var levels = new int[network.VertexCount];
        Array.Fill(levels, -1);
        levels[source] = 0;

        var queue = new Queue<int>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var arc in network.IncidentArcs(current))
            {
                var head = network.Head(arc);
                if (levels[head] < 0 && network.Residual(arc) > TWeight.Zero)
                {
                    levels[head] = levels[current] + 1;
                    queue.Enqueue(head);
                }
            }
        }

        return levels;
    }

    /// <summary>
    /// DFS phase: repeatedly walk level-increasing positive-residual arcs from
    /// the source, pushing the bottleneck whenever the sink is reached. Arc
    /// cursors never rewind within a phase, so each arc is examined once and
    /// the phase yields a blocking flow.
    /// </summary>
    private static TWeight BlockingFlow(
        ResidualNetwork<TVertex, TEdge, TWeight> network,
        int[] levels,
        int source,
        int sink)
    {
        var cursors = new int[network.VertexCount];
        var path = new List<int>();
        var total = TWeight.Zero;
        var current = source;

        while (true)
        {
            if (current == sink)
            {
                var bottleneck = network.Residual(path[0]);
                for (var i = 1; i < path.Count; i++)
                {
                    bottleneck = TWeight.Min(bottleneck, network.Residual(path[i]));
                }

                foreach (var arc in path)
                {
                    network.Push(arc, bottleneck);
                }

                total += bottleneck;

                // Retreat to the tail of the first saturated arc on the path.
                var truncateAt = path.FindIndex(arc => network.Residual(arc) <= TWeight.Zero);
                path.RemoveRange(truncateAt, path.Count - truncateAt);
                current = path.Count > 0 ? network.Head(path[^1]) : source;
                continue;
            }

            var advanced = false;
            var arcs = network.IncidentArcs(current);
            while (cursors[current] < arcs.Count)
            {
                var arc = arcs[cursors[current]];
                var head = network.Head(arc);
                if (network.Residual(arc) > TWeight.Zero && levels[head] == levels[current] + 1)
                {
                    path.Add(arc);
                    current = head;
                    advanced = true;
                    break;
                }

                cursors[current]++;
            }

            if (advanced)
            {
                continue;
            }

            if (current == source)
            {
                return total; // the level graph is saturated
            }

            // Dead end: drop the vertex from this phase and backtrack past
            // the arc that led here.
            levels[current] = -1;
            path.RemoveAt(path.Count - 1);
            current = path.Count > 0 ? network.Head(path[^1]) : source;
            cursors[current]++;
        }
    }
}
