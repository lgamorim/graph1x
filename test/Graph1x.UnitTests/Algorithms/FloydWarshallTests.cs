using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class FloydWarshallTests
{
    private static DirectedGraph<string, WeightedEdge<string, int>> Directed(
        params (string Source, string Target, int Weight)[] edges)
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, weight) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, weight));
        }

        return graph;
    }

    private static FloydWarshallAllShortestPaths<string, WeightedEdge<string, int>, int> FloydWarshall()
        => new(edge => edge.Weight);

    [Fact]
    public void Compute_SmallFixture_YieldsKnownDistances()
    {
        var graph = Directed(("a", "b", 3), ("b", "c", 2), ("a", "c", 7), ("c", "a", 1));

        var paths = FloydWarshall().Compute(graph);

        Assert.Equal(3, paths.Between("a", "b").Distance);
        Assert.Equal(5, paths.Between("a", "c").Distance);
        Assert.Equal(4, paths.Between("c", "b").Distance); // c -> a -> b
        Assert.Equal(3, paths.Between("b", "a").Distance);
        Assert.Equal(0, paths.Between("a", "a").Distance);
    }

    [Fact]
    public void Compute_PathReconstruction_IsConsistent()
    {
        var graph = Directed(("a", "b", 3), ("b", "c", 2), ("a", "c", 7));

        var result = FloydWarshall().Compute(graph).Between("a", "c");

        Assert.Equal(["a", "b", "c"], result.Path);
        Assert.Equal("a", result.Source);
        Assert.Equal("c", result.Target);
    }

    [Fact]
    public void Compute_UnreachablePair_ReportsUnreachable()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("island");

        var result = FloydWarshall().Compute(graph).Between("a", "island");

        Assert.False(result.IsReachable);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void Compute_NegativeEdgesWithoutCycle_AreSupported()
    {
        var graph = Directed(("a", "b", 3), ("b", "c", -2));

        Assert.Equal(1, FloydWarshall().Compute(graph).Between("a", "c").Distance);
    }

    [Fact]
    public void Compute_NegativeCycle_Throws()
    {
        var graph = Directed(("a", "b", 1), ("b", "a", -2));

        Assert.Throws<NegativeCycleException>(() => FloydWarshall().Compute(graph));
    }

    [Fact]
    public void Compute_UndirectedGraph_TreatsEdgesBothWays()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 3));

        var paths = FloydWarshall().Compute(graph);

        Assert.Equal(5, paths.Between("c", "a").Distance);
        Assert.Equal(5, paths.Between("a", "c").Distance);
    }

    [Fact]
    public void Between_UnknownVertex_Throws()
    {
        var graph = Directed(("a", "b", 1));
        var paths = FloydWarshall().Compute(graph);

        Assert.Throws<ArgumentException>(() => paths.Between("ghost", "b"));
        Assert.Throws<ArgumentException>(() => paths.Between("a", "ghost"));
    }

    [Fact]
    public void Compute_AgreesWithDijkstra_OnSeededRandomGraph()
    {
        var random = new Random(20260702);
        var graph = new DirectedGraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < 12; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 30; i++)
        {
            graph.AddEdge(new WeightedEdge<int, int>(random.Next(12), random.Next(12), random.Next(1, 20)));
        }

        var allPairs = new FloydWarshallAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .Compute(graph);
        var dijkstra = new DijkstraShortestPath<int, WeightedEdge<int, int>, int>(e => e.Weight);

        foreach (var source in graph.Vertices)
        {
            foreach (var target in graph.Vertices)
            {
                var expected = dijkstra.FindPath(graph, source, target);
                var actual = allPairs.Between(source, target);

                Assert.Equal(expected.IsReachable, actual.IsReachable);
                if (expected.IsReachable)
                {
                    Assert.Equal(expected.Distance, actual.Distance);
                }
            }
        }
    }
}
