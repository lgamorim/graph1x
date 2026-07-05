using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class CancellationTests
{
    private static DirectedGraph<int, WeightedEdge<int, int>> Network()
    {
        var random = new Random(20260705);
        var graph = new DirectedGraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < 20; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 80; i++)
        {
            var a = random.Next(20);
            var b = random.Next(20);
            if (a != b)
            {
                graph.AddEdge(new WeightedEdge<int, int>(a, b, random.Next(1, 10)));
            }
        }

        return graph;
    }

    public static TheoryData<string> CancellableOperations => new(
        "FloydWarshall", "DijkstraPathsFrom", "BellmanFordPathsFrom", "ShortestPathsFromFacade",
        "Betweenness", "BetweennessWeighted", "Closeness", "ClosenessWeighted", "PageRank",
        "EdmondsKarp", "Dinic", "MaximumFlowFacade", "TransitiveClosure", "TransitiveReduction",
        "Condense", "Diameter", "AveragePathLength");

    private static void Run(string operation, IDirectedGraph<int, WeightedEdge<int, int>> graph, CancellationToken token)
    {
        // A strongly connected ring keeps metrics happy where required.
        var ring = new DirectedGraph<int, WeightedEdge<int, int>>();
        for (var i = 0; i < 10; i++)
        {
            ring.AddEdge(new WeightedEdge<int, int>(i, (i + 1) % 10, 1));
        }

        var dag = new DirectedGraph<int, WeightedEdge<int, int>>();
        dag.AddEdge(new WeightedEdge<int, int>(0, 1, 1));
        dag.AddEdge(new WeightedEdge<int, int>(1, 2, 1));
        dag.AddEdge(new WeightedEdge<int, int>(0, 2, 1));

        switch (operation)
        {
            case "FloydWarshall":
                new FloydWarshallAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
                    .Compute(graph, token);
                break;
            case "DijkstraPathsFrom":
                new DijkstraShortestPath<int, WeightedEdge<int, int>, int>(e => e.Weight)
                    .FindPathsFrom(graph, 0, token);
                break;
            case "BellmanFordPathsFrom":
                new BellmanFordShortestPath<int, WeightedEdge<int, int>, int>(e => e.Weight)
                    .FindPathsFrom(graph, 0, token);
                break;
            case "ShortestPathsFromFacade":
                graph.ShortestPathsFrom(0, e => e.Weight, token);
                break;
            case "Betweenness":
                graph.BetweennessCentrality(token);
                break;
            case "BetweennessWeighted":
                graph.BetweennessCentrality(e => e.Weight, token);
                break;
            case "Closeness":
                graph.ClosenessCentrality(token);
                break;
            case "ClosenessWeighted":
                graph.ClosenessCentrality(e => e.Weight, token);
                break;
            case "PageRank":
                graph.PageRank(0.85, 100, 1e-9, token);
                break;
            case "EdmondsKarp":
                new EdmondsKarpMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
                    .FindMaximumFlow(graph, 0, 19, token);
                break;
            case "Dinic":
                new DinicMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
                    .FindMaximumFlow(graph, 0, 19, token);
                break;
            case "MaximumFlowFacade":
                graph.MaximumFlow(0, 19, e => e.Weight, token);
                break;
            case "TransitiveClosure":
                graph.TransitiveClosure((s, t) => new WeightedEdge<int, int>(s, t, 0), token);
                break;
            case "TransitiveReduction":
                dag.TransitiveReduction(token);
                break;
            case "Condense":
                graph.Condense(token);
                break;
            case "Diameter":
                ring.Diameter(e => e.Weight, token);
                break;
            case "AveragePathLength":
                ring.AveragePathLength(e => e.Weight, token);
                break;
            default:
                Assert.Fail($"Unknown operation '{operation}'.");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(CancellableOperations))]
    public void PreCancelledToken_ThrowsImmediately(string operation)
    {
        var graph = Network();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => Run(operation, graph, source.Token));
    }

    [Theory]
    [MemberData(nameof(CancellableOperations))]
    public void NoneToken_ProducesSameBehaviorAsTokenlessCall(string operation)
    {
        var graph = Network();

        Run(operation, graph, CancellationToken.None); // must complete without throwing
    }

    [Fact]
    public void MidComputation_CancellationFromWeightSelector_IsObserved()
    {
        var graph = Network();
        using var source = new CancellationTokenSource();
        var calls = 0;
        var algorithm = new DijkstraShortestPath<int, WeightedEdge<int, int>, int>(edge =>
        {
            if (++calls == 25)
            {
                source.Cancel();
            }

            return edge.Weight;
        });

        Assert.Throws<OperationCanceledException>(() => algorithm.FindPathsFrom(graph, 0, source.Token));
        Assert.True(calls >= 25);
    }

    [Fact]
    public void MidComputation_BetweennessObservesCancellationBetweenSources()
    {
        var graph = Network();
        using var source = new CancellationTokenSource();
        var calls = 0;

        Assert.Throws<OperationCanceledException>(() => graph.BetweennessCentrality(
            edge =>
            {
                if (++calls == 100)
                {
                    source.Cancel();
                }

                return edge.Weight;
            },
            source.Token));
    }

    [Fact]
    public void MidComputation_FlowObservesCancellationFromCapacitySelector()
    {
        var graph = Network();
        using var source = new CancellationTokenSource();
        var calls = 0;
        var algorithm = new EdmondsKarpMaximumFlow<int, WeightedEdge<int, int>, int>(edge =>
        {
            if (++calls == 40)
            {
                source.Cancel();
            }

            return edge.Weight;
        });

        Assert.Throws<OperationCanceledException>(() => algorithm.FindMaximumFlow(graph, 0, 19, source.Token));
    }

    [Fact]
    public void TokenOverloads_MatchTokenlessResults()
    {
        var graph = Network();

        Assert.Equal(
            graph.PageRank(),
            graph.PageRank(0.85, 100, 1e-9, CancellationToken.None));
        Assert.Equal(
            graph.BetweennessCentrality(),
            graph.BetweennessCentrality(CancellationToken.None));
        Assert.Equal(
            graph.MaximumFlow(0, 19, e => e.Weight).FlowValue,
            graph.MaximumFlow(0, 19, e => e.Weight, CancellationToken.None).FlowValue);
    }

    [Fact]
    public void InterfaceDefaultImplementation_ForwardsWhenNotOverridden()
    {
        // A minimal third-party implementation that only provides the
        // tokenless member still works through the token overload (the
        // default interface implementation forwards and documents that the
        // token is ignored unless overridden).
        IMaximumFlowAlgorithm<int, WeightedEdge<int, int>, int> custom = new TokenlessFlow();
        var graph = Network();

        var result = custom.FindMaximumFlow(graph, 0, 19, CancellationToken.None);

        Assert.Equal(123, result.FlowValue);
    }

    private sealed class TokenlessFlow : IMaximumFlowAlgorithm<int, WeightedEdge<int, int>, int>
    {
        public MaximumFlowResult<int, WeightedEdge<int, int>, int> FindMaximumFlow(
            IDirectedGraph<int, WeightedEdge<int, int>> graph,
            int source,
            int sink)
            => new EdmondsKarpMaximumFlow<int, WeightedEdge<int, int>, int>(_ => 123)
                .FindMaximumFlow(
                    new DirectedGraph<int, WeightedEdge<int, int>>().Also(g => g.AddEdge(new WeightedEdge<int, int>(source, sink, 123))),
                    source,
                    sink);
    }
}

/// <summary>Tiny fluent helper for building inline fixtures.</summary>
internal static class FixtureExtensions
{
    public static T Also<T>(this T value, Action<T> configure)
    {
        configure(value);
        return value;
    }
}
