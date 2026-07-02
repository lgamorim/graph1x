using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class DijkstraTests
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

    private static DijkstraShortestPath<string, WeightedEdge<string, int>, int> Dijkstra()
        => new(edge => edge.Weight);

    [Fact]
    public void FindPath_SimpleChain_ReturnsDistanceAndPath()
    {
        var graph = Directed(("a", "b", 2), ("b", "c", 3));

        var result = Dijkstra().FindPath(graph, "a", "c");

        Assert.True(result.IsReachable);
        Assert.Equal(5, result.Distance);
        Assert.Equal(["a", "b", "c"], result.Path);
    }

    [Fact]
    public void FindPath_PrefersLighterMultiHopOverHeavyDirectEdge()
    {
        var graph = Directed(("a", "c", 10), ("a", "b", 2), ("b", "c", 3));

        var result = Dijkstra().FindPath(graph, "a", "c");

        Assert.Equal(5, result.Distance);
        Assert.Equal(["a", "b", "c"], result.Path);
    }

    [Fact]
    public void FindPath_UnreachableTarget_ReportsUnreachable()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("island");

        var result = Dijkstra().FindPath(graph, "a", "island");

        Assert.False(result.IsReachable);
        Assert.Empty(result.Path);
        Assert.Throws<InvalidOperationException>(() => result.Distance);
    }

    [Fact]
    public void FindPath_RespectsEdgeDirection()
    {
        var graph = Directed(("a", "b", 1));

        Assert.False(Dijkstra().FindPath(graph, "b", "a").IsReachable);
    }

    [Fact]
    public void FindPath_SourceEqualsTarget_IsZeroLengthPath()
    {
        var graph = Directed(("a", "b", 1));

        var result = Dijkstra().FindPath(graph, "a", "a");

        Assert.True(result.IsReachable);
        Assert.Equal(0, result.Distance);
        Assert.Equal(["a"], result.Path);
    }

    [Fact]
    public void FindPath_NegativeWeight_Throws()
    {
        var graph = Directed(("a", "b", -1));

        Assert.Throws<NegativeWeightException>(() => Dijkstra().FindPath(graph, "a", "b"));
    }

    [Fact]
    public void FindPath_MissingSource_Throws()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Throws<ArgumentException>(() => Dijkstra().FindPath(graph, "ghost", "b"));
    }

    [Fact]
    public void FindPath_MissingTarget_Throws()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Throws<ArgumentException>(() => Dijkstra().FindPath(graph, "a", "ghost"));
    }

    [Fact]
    public void FindPath_UndirectedGraph_TraversesBothWays()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 4));
        graph.AddEdge(new WeightedEdge<string, int>("c", "b", 1));

        var result = Dijkstra().FindPath(graph, "a", "c");

        Assert.Equal(5, result.Distance);
        Assert.Equal(["a", "b", "c"], result.Path);
    }

    [Fact]
    public void FindPath_MultigraphParallelEdges_UsesCheapest()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 9));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        Assert.Equal(2, Dijkstra().FindPath(graph, "a", "b").Distance);
    }

    [Fact]
    public void FindPath_SelfLoop_DoesNotDisturbResult()
    {
        var graph = Directed(("a", "a", 5), ("a", "b", 1));

        Assert.Equal(1, Dijkstra().FindPath(graph, "a", "b").Distance);
    }

    [Fact]
    public void FindPath_DecimalWeights_UseGenericMath()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, decimal>>();
        graph.AddEdge(new WeightedEdge<string, decimal>("a", "b", 0.1m));
        graph.AddEdge(new WeightedEdge<string, decimal>("b", "c", 0.2m));

        var algorithm = new DijkstraShortestPath<string, WeightedEdge<string, decimal>, decimal>(e => e.Weight);

        Assert.Equal(0.3m, algorithm.FindPath(graph, "a", "c").Distance);
    }

    [Fact]
    public void FindPath_ZeroWeightEdges_AreValid()
    {
        var graph = Directed(("a", "b", 0), ("b", "c", 0));

        Assert.Equal(0, Dijkstra().FindPath(graph, "a", "c").Distance);
    }

    [Fact]
    public void Result_ExposesSourceAndTarget()
    {
        var graph = Directed(("a", "b", 1));

        var result = Dijkstra().FindPath(graph, "a", "b");

        Assert.Equal("a", result.Source);
        Assert.Equal("b", result.Target);
    }
}
