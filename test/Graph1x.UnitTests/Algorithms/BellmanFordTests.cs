using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class BellmanFordTests
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

    private static BellmanFordShortestPath<string, WeightedEdge<string, int>, int> BellmanFord()
        => new(edge => edge.Weight);

    [Fact]
    public void FindPath_NegativeEdges_FindsTrueShortestPath()
    {
        // The direct a->c edge (2) loses to a->b->c (3 + -2 = 1).
        var graph = Directed(("a", "c", 2), ("a", "b", 3), ("b", "c", -2));

        var result = BellmanFord().FindPath(graph, "a", "c");

        Assert.Equal(1, result.Distance);
        Assert.Equal(["a", "b", "c"], result.Path);
    }

    [Fact]
    public void FindPath_NegativeCycleReachableFromSource_Throws()
    {
        var graph = Directed(("a", "b", 1), ("b", "c", -3), ("c", "b", 1), ("c", "d", 1));

        Assert.Throws<NegativeCycleException>(() => BellmanFord().FindPath(graph, "a", "d"));
    }

    [Fact]
    public void FindPath_NegativeCycleNotReachableFromSource_IsIgnored()
    {
        var graph = Directed(("a", "b", 1), ("x", "y", -5), ("y", "x", -5));

        var result = BellmanFord().FindPath(graph, "a", "b");

        Assert.Equal(1, result.Distance);
    }

    [Fact]
    public void FindPath_NegativeSelfLoop_IsANegativeCycle()
    {
        var graph = Directed(("a", "b", 1), ("b", "b", -1));

        Assert.Throws<NegativeCycleException>(() => BellmanFord().FindPath(graph, "a", "b"));
    }

    [Fact]
    public void FindPath_NonNegativeGraph_AgreesWithDijkstra()
    {
        var graph = Directed(
            ("a", "b", 4), ("a", "c", 1), ("c", "b", 2), ("b", "d", 5), ("c", "d", 8), ("d", "e", 3));

        var dijkstra = new DijkstraShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight);

        foreach (var target in new[] { "b", "c", "d", "e" })
        {
            Assert.Equal(
                dijkstra.FindPath(graph, "a", target).Distance,
                BellmanFord().FindPath(graph, "a", target).Distance);
        }
    }

    [Fact]
    public void FindPath_UnreachableTarget_ReportsUnreachable()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("island");

        Assert.False(BellmanFord().FindPath(graph, "a", "island").IsReachable);
    }

    [Fact]
    public void FindPath_UndirectedNegativeEdge_IsANegativeCycle()
    {
        // Any negative undirected edge can be traversed back and forth forever.
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", -1));

        Assert.Throws<NegativeCycleException>(() => BellmanFord().FindPath(graph, "a", "b"));
    }

    [Fact]
    public void FindPath_MissingEndpoints_Throw()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Throws<ArgumentException>(() => BellmanFord().FindPath(graph, "ghost", "b"));
        Assert.Throws<ArgumentException>(() => BellmanFord().FindPath(graph, "a", "ghost"));
    }

    [Fact]
    public void FindPath_SourceEqualsTarget_IsZero()
    {
        var graph = Directed(("a", "b", -1));

        Assert.Equal(0, BellmanFord().FindPath(graph, "a", "a").Distance);
    }
}
