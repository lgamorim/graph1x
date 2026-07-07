using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class MinCostFlowTests
{
    private sealed record CostEdge(string Source, string Target, int Capacity, int Cost) : IEdge<string>;

    private static DirectedGraph<string, CostEdge> Network(params CostEdge[] edges)
    {
        var graph = new DirectedGraph<string, CostEdge>();
        foreach (var edge in edges)
        {
            graph.AddEdge(edge);
        }

        return graph;
    }

    private static MinCostMaximumFlow<string, CostEdge, int> Algorithm()
        => new(edge => edge.Capacity, edge => edge.Cost);

    [Fact]
    public void FindMinimumCostMaximumFlow_SimpleNetwork_ComputesFlowValueAndTotalCost()
    {
        var graph = Network(
            new CostEdge("s", "a", 2, 1),
            new CostEdge("s", "b", 2, 2),
            new CostEdge("a", "t", 2, 1),
            new CostEdge("b", "t", 2, 1));

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(4, result.FlowValue);
        Assert.Equal(10, result.TotalCost); // 2·(1+1) over a, 2·(2+1) over b
        Assert.Equal("s", result.Source);
        Assert.Equal("t", result.Sink);
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_TwoRoutesOneBottleneck_PicksTheCheaperRoute()
    {
        var graph = Network(
            new CostEdge("s", "a", 1, 1),
            new CostEdge("a", "m", 1, 1),
            new CostEdge("s", "b", 1, 100),
            new CostEdge("b", "m", 1, 100),
            new CostEdge("m", "t", 1, 0));

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(1, result.FlowValue);
        Assert.Equal(2, result.TotalCost);
        var expensive = result.EdgeFlows.Where(pair => pair.Edge.Cost == 100);
        Assert.All(expensive, pair => Assert.Equal(0, pair.Flow));
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_NegativeCostsWithoutNegativeCycle_AreSupported()
    {
        var graph = Network(
            new CostEdge("s", "a", 1, -5),
            new CostEdge("a", "t", 1, 2));

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(1, result.FlowValue);
        Assert.Equal(-3, result.TotalCost);
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_NegativeCostCycle_ThrowsNegativeCycleException()
    {
        var graph = Network(
            new CostEdge("s", "a", 1, 1),
            new CostEdge("a", "b", 1, -3),
            new CostEdge("b", "a", 1, 1),
            new CostEdge("b", "t", 1, 1));

        Assert.Throws<NegativeCycleException>(() => Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t"));
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_NegativeCapacity_ThrowsNegativeWeightException()
    {
        var graph = Network(new CostEdge("s", "t", -1, 1));

        Assert.Throws<NegativeWeightException>(() => Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t"));
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_UnreachableSink_YieldsZeroFlowAndZeroCost()
    {
        var graph = Network(new CostEdge("s", "a", 3, 2));
        graph.AddVertex("t");

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(0, result.FlowValue);
        Assert.Equal(0, result.TotalCost);
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_SelfLoops_AreIgnored()
    {
        var graph = Network(
            new CostEdge("s", "s", 5, -100),
            new CostEdge("s", "t", 1, 3));

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(1, result.FlowValue);
        Assert.Equal(3, result.TotalCost);
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_ParallelEdges_CarryIndividualFlows()
    {
        var graph = new DirectedMultigraph<string, CostEdge>();
        graph.AddEdge(new CostEdge("s", "t", 1, 1));
        graph.AddEdge(new CostEdge("s", "t", 1, 4));

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(2, result.FlowValue);
        Assert.Equal(5, result.TotalCost);
        Assert.Equal(2, result.EdgeFlows.Count);
        Assert.All(result.EdgeFlows, pair => Assert.Equal(1, pair.Flow));
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_SourceEqualsSink_ThrowsArgumentException()
    {
        var graph = Network(new CostEdge("s", "t", 1, 1));

        Assert.Throws<ArgumentException>(() => Algorithm().FindMinimumCostMaximumFlow(graph, "s", "s"));
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_FlowValueMatchesEdmondsKarp_OnRandomNetworks()
    {
        foreach (var seed in new[] { 3, 17, 42 })
        {
            var random = GraphGenerator.ErdosRenyiDirected(24, 0.12, seed);
            var graph = new DirectedGraph<string, CostEdge>();
            foreach (var vertex in random.Vertices)
            {
                graph.AddVertex(vertex.ToString());
            }

            foreach (var edge in random.Edges)
            {
                graph.AddEdge(new CostEdge(
                    edge.Source.ToString(),
                    edge.Target.ToString(),
                    (edge.Source * 31 + edge.Target) % 5 + 1,
                    (edge.Source * 7 + edge.Target) % 9));
            }

            var expected = graph.MaximumFlow("0", "23", edge => edge.Capacity).FlowValue;
            var result = Algorithm().FindMinimumCostMaximumFlow(graph, "0", "23");

            Assert.Equal(expected, result.FlowValue);
        }
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_TotalCostEqualsSumOfEdgeFlowsTimesCost()
    {
        var random = GraphGenerator.ErdosRenyiDirected(24, 0.12, seed: 42);
        var graph = new DirectedGraph<string, CostEdge>();
        foreach (var vertex in random.Vertices)
        {
            graph.AddVertex(vertex.ToString());
        }

        foreach (var edge in random.Edges)
        {
            graph.AddEdge(new CostEdge(
                edge.Source.ToString(),
                edge.Target.ToString(),
                (edge.Source * 31 + edge.Target) % 5 + 1,
                (edge.Source * 7 + edge.Target) % 9));
        }

        var result = Algorithm().FindMinimumCostMaximumFlow(graph, "0", "23");

        var recomputed = result.EdgeFlows.Sum(pair => pair.Flow * pair.Edge.Cost);
        Assert.Equal(result.TotalCost, recomputed);
    }

    [Fact]
    public void FindMinimumCostMaximumFlow_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var graph = Network(new CostEdge("s", "t", 1, 1));
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t", source.Token));
    }

    [Fact]
    public void MinimumCostMaximumFlow_ExtensionMethod_MatchesTheAlgorithmClass()
    {
        var graph = Network(
            new CostEdge("s", "a", 2, 1),
            new CostEdge("a", "t", 2, 3));

        var viaExtension = graph.MinimumCostMaximumFlow("s", "t", edge => edge.Capacity, edge => edge.Cost);
        var viaClass = Algorithm().FindMinimumCostMaximumFlow(graph, "s", "t");

        Assert.Equal(viaClass.FlowValue, viaExtension.FlowValue);
        Assert.Equal(viaClass.TotalCost, viaExtension.TotalCost);
    }

    [Fact]
    public void MinCostMaximumFlow_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MinCostMaximumFlow<string, CostEdge, int>(null!, edge => edge.Cost));
        Assert.Throws<ArgumentNullException>(
            () => new MinCostMaximumFlow<string, CostEdge, int>(edge => edge.Capacity, null!));
        Assert.Throws<ArgumentNullException>(
            () => Algorithm().FindMinimumCostMaximumFlow(null!, "s", "t"));
    }
}
