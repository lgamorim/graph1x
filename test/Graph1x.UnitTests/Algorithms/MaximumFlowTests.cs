using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class MaximumFlowTests
{
    private static DirectedGraph<string, WeightedEdge<string, int>> Directed(
        params (string Source, string Target, int Capacity)[] edges)
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, capacity) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, capacity));
        }

        return graph;
    }

    /// <summary>Both strategies must satisfy every fixture in this suite.</summary>
    private static IEnumerable<IMaximumFlowAlgorithm<string, WeightedEdge<string, int>, int>> Algorithms()
    {
        yield return new EdmondsKarpMaximumFlow<string, WeightedEdge<string, int>, int>(e => e.Weight);
        yield return new DinicMaximumFlow<string, WeightedEdge<string, int>, int>(e => e.Weight);
    }

    /// <summary>Asserts flow conservation, capacity constraints, and that the min cut certifies the flow value.</summary>
    private static void AssertFlowIsValid(
        IDirectedGraph<string, WeightedEdge<string, int>> graph,
        MaximumFlowResult<string, WeightedEdge<string, int>, int> result)
    {
        foreach (var (edge, flow) in result.EdgeFlows)
        {
            Assert.InRange(flow, 0, edge.Weight);
        }

        var net = graph.Vertices.ToDictionary(v => v, _ => 0);
        foreach (var (edge, flow) in result.EdgeFlows)
        {
            net[edge.Source] -= flow;
            net[edge.Target] += flow;
        }

        foreach (var vertex in graph.Vertices)
        {
            if (vertex != result.Source && vertex != result.Sink)
            {
                Assert.Equal(0, net[vertex]);
            }
        }

        Assert.Equal(result.FlowValue, net[result.Sink]);
        Assert.Equal(result.FlowValue, result.MinCutEdges.Sum(edge => edge.Weight));
        Assert.Contains(result.Source, result.SourceSideOfMinCut);
        Assert.DoesNotContain(result.Sink, result.SourceSideOfMinCut);
        Assert.All(result.MinCutEdges, edge =>
        {
            Assert.Contains(edge.Source, result.SourceSideOfMinCut);
            Assert.DoesNotContain(edge.Target, result.SourceSideOfMinCut);
        });
    }

    [Fact]
    public void SingleEdge_FlowEqualsCapacity()
    {
        var graph = Directed(("s", "t", 7));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(7, result.FlowValue);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void Chain_FlowEqualsBottleneck()
    {
        var graph = Directed(("s", "a", 9), ("a", "b", 3), ("b", "t", 5));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(3, result.FlowValue);
            Assert.Single(result.MinCutEdges);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void ClassicNetwork_HasKnownMaxFlow()
    {
        // CLRS figure 26.1: max flow 23.
        var graph = Directed(
            ("s", "v1", 16), ("s", "v2", 13),
            ("v1", "v3", 12), ("v2", "v1", 4), ("v2", "v4", 14),
            ("v3", "v2", 9), ("v3", "t", 20),
            ("v4", "v3", 7), ("v4", "t", 4));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(23, result.FlowValue);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void Diamond_SplitsFlowAcrossBranches()
    {
        var graph = Directed(("s", "a", 4), ("s", "b", 3), ("a", "t", 5), ("b", "t", 2));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(6, result.FlowValue);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void ParallelEdges_CapacitiesAdd()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("s", "t", 3));
        graph.AddEdge(new WeightedEdge<string, int>("s", "t", 4));

        foreach (var algorithm in Algorithms())
        {
            Assert.Equal(7, algorithm.FindMaximumFlow(graph, "s", "t").FlowValue);
        }
    }

    [Fact]
    public void AntiParallelEdges_AreHandledIndependently()
    {
        var graph = Directed(("s", "a", 10), ("a", "t", 10), ("a", "s", 5), ("t", "a", 5));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(10, result.FlowValue);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void UnreachableSink_HasZeroFlowAndEmptyCut()
    {
        var graph = Directed(("s", "a", 5));
        graph.AddVertex("t");

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(0, result.FlowValue);
            Assert.Empty(result.MinCutEdges);
            Assert.DoesNotContain("t", result.SourceSideOfMinCut);
        }
    }

    [Fact]
    public void ZeroCapacityEdge_CarriesNoFlow()
    {
        var graph = Directed(("s", "t", 0), ("s", "a", 2), ("a", "t", 2));

        foreach (var algorithm in Algorithms())
        {
            var result = algorithm.FindMaximumFlow(graph, "s", "t");

            Assert.Equal(2, result.FlowValue);
            AssertFlowIsValid(graph, result);
        }
    }

    [Fact]
    public void SelfLoops_AreIgnored()
    {
        var graph = Directed(("s", "s", 9), ("s", "t", 2), ("t", "t", 9));

        foreach (var algorithm in Algorithms())
        {
            Assert.Equal(2, algorithm.FindMaximumFlow(graph, "s", "t").FlowValue);
        }
    }

    [Fact]
    public void NegativeCapacity_Throws()
    {
        var graph = Directed(("s", "t", -1));

        foreach (var algorithm in Algorithms())
        {
            Assert.Throws<NegativeWeightException>(() => algorithm.FindMaximumFlow(graph, "s", "t"));
        }
    }

    [Fact]
    public void SourceEqualsSink_Throws()
    {
        var graph = Directed(("s", "t", 1));

        foreach (var algorithm in Algorithms())
        {
            Assert.Throws<ArgumentException>(() => algorithm.FindMaximumFlow(graph, "s", "s"));
        }
    }

    [Fact]
    public void MissingEndpoints_Throw()
    {
        var graph = Directed(("s", "t", 1));

        foreach (var algorithm in Algorithms())
        {
            Assert.Throws<ArgumentException>(() => algorithm.FindMaximumFlow(graph, "ghost", "t"));
            Assert.Throws<ArgumentException>(() => algorithm.FindMaximumFlow(graph, "s", "ghost"));
        }
    }

    [Fact]
    public void DoubleCapacities_UseGenericMath()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("s", "a", 1.5));
        graph.AddEdge(new WeightedEdge<string, double>("a", "t", 0.5));

        Assert.Equal(0.5, new EdmondsKarpMaximumFlow<string, WeightedEdge<string, double>, double>(e => e.Weight)
            .FindMaximumFlow(graph, "s", "t").FlowValue);
        Assert.Equal(0.5, new DinicMaximumFlow<string, WeightedEdge<string, double>, double>(e => e.Weight)
            .FindMaximumFlow(graph, "s", "t").FlowValue);
    }

    [Fact]
    public void Facade_MaximumFlow_WorksWithAndWithoutSelector()
    {
        var graph = Directed(("s", "a", 3), ("a", "t", 2));

        Assert.Equal(2, graph.MaximumFlow("s", "t").FlowValue);
        Assert.Equal(2, graph.MaximumFlow("s", "t", edge => edge.Weight).FlowValue);
    }

    [Fact]
    public void EdmondsKarpAndDinic_AgreeOnSeededRandomNetworks()
    {
        var random = new Random(20260705);
        for (var round = 0; round < 5; round++)
        {
            var graph = new DirectedMultigraph<int, WeightedEdge<int, int>>();
            for (var v = 0; v < 12; v++)
            {
                graph.AddVertex(v);
            }

            for (var i = 0; i < 40; i++)
            {
                var a = random.Next(12);
                var b = random.Next(12);
                if (a != b)
                {
                    graph.AddEdge(new WeightedEdge<int, int>(a, b, random.Next(1, 15)));
                }
            }

            var edmondsKarp = new EdmondsKarpMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
                .FindMaximumFlow(graph, 0, 11);
            var dinic = new DinicMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
                .FindMaximumFlow(graph, 0, 11);

            Assert.Equal(edmondsKarp.FlowValue, dinic.FlowValue);
            Assert.Equal(edmondsKarp.MinCutEdges.Sum(e => e.Weight), dinic.MinCutEdges.Sum(e => e.Weight));
        }
    }
}
