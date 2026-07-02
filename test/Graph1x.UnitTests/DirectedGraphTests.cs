using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests;

public class DirectedGraphTests : SimpleGraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new DirectedGraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new DirectedGraph<string, Edge<string>>(comparer);

    private static DirectedGraph<string, Edge<string>> CreateDirected() => new();

    [Fact]
    public void IsDirected_IsTrue()
    {
        Assert.True(CreateGraph().IsDirected);
    }

    [Fact]
    public void ContainsEdge_RespectsDirection()
    {
        var graph = CreateDirected();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.ContainsEdge("a", "b"));
        Assert.False(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void AddEdge_ReverseDirection_IsDistinctEdge()
    {
        var graph = CreateDirected();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.AddEdge(new Edge<string>("b", "a")));
        Assert.Equal(2, graph.EdgeCount);
    }

    [Fact]
    public void OutDegree_And_InDegree_CountDirectedEdges()
    {
        var graph = CreateDirected();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "c"));
        graph.AddEdge(new Edge<string>("c", "a"));

        Assert.Equal(2, graph.OutDegree("a"));
        Assert.Equal(1, graph.InDegree("a"));
        Assert.Equal(3, graph.Degree("a"));
        Assert.Equal(1, graph.InDegree("b"));
        Assert.Equal(0, graph.OutDegree("b"));
    }

    [Fact]
    public void SelfLoop_CountsOneInAndOneOut()
    {
        var graph = CreateDirected();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Equal(1, graph.OutDegree("a"));
        Assert.Equal(1, graph.InDegree("a"));
        Assert.Equal(2, graph.Degree("a"));
    }

    [Fact]
    public void OutEdges_And_InEdges_ReturnDirectedNeighborhoods()
    {
        var graph = CreateDirected();
        var ab = new Edge<string>("a", "b");
        var ca = new Edge<string>("c", "a");
        graph.AddEdge(ab);
        graph.AddEdge(ca);

        Assert.Equal([ab], graph.OutEdges("a"));
        Assert.Equal([ca], graph.InEdges("a"));
    }

    [Fact]
    public void OutEdges_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateDirected().OutEdges("ghost").ToList());
    }

    [Fact]
    public void InEdges_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateDirected().InEdges("ghost").ToList());
    }

    [Fact]
    public void AdjacentEdges_CombinesInAndOut()
    {
        var graph = CreateDirected();
        var ab = new Edge<string>("a", "b");
        var ca = new Edge<string>("c", "a");
        graph.AddEdge(ab);
        graph.AddEdge(ca);

        var adjacent = graph.AdjacentEdges("a").ToList();

        Assert.Equal(2, adjacent.Count);
        Assert.Contains(ab, adjacent);
        Assert.Contains(ca, adjacent);
    }

    [Fact]
    public void RemoveVertex_UpdatesNeighborDegrees()
    {
        var graph = CreateDirected();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("c", "b"));

        graph.RemoveVertex("b");

        Assert.Equal(0, graph.OutDegree("a"));
        Assert.Equal(0, graph.OutDegree("c"));
    }

    [Fact]
    public void WeightedEdges_SameEndpointsDifferentWeight_StillRejected()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));

        Assert.False(graph.AddEdge(new WeightedEdge<string, int>("a", "b", 9)));
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void RemoveEdge_ByEndpoints_RemovesWhateverConnectsThem()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        Assert.True(graph.RemoveEdge("a", "b"));
        Assert.Equal(0, graph.EdgeCount);
        Assert.False(graph.RemoveEdge("a", "b"));
    }

    [Fact]
    public void RemoveEdge_ExactMatch_RequiresFullEquality()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        Assert.False(graph.RemoveEdge(new WeightedEdge<string, int>("a", "b", 8)));
        Assert.True(graph.RemoveEdge(new WeightedEdge<string, int>("a", "b", 7)));
    }
}
