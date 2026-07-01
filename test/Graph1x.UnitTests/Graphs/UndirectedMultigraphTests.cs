using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

public class UndirectedMultigraphTests : MultigraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new UndirectedMultigraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new UndirectedMultigraph<string, Edge<string>>(comparer);

    [Fact]
    public void IsDirected_IsFalse()
    {
        Assert.False(CreateGraph().IsDirected);
    }

    [Fact]
    public void ContainsEdge_IsSymmetric()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.ContainsEdge("a", "b"));
        Assert.True(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void ParallelEdges_WithOppositeOrientations_Coexist()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "a"));

        Assert.Equal(2, graph.EdgeCount);
        Assert.Equal(2, graph.Degree("a"));
        Assert.Equal(2, graph.Edges.Count());
    }

    [Fact]
    public void GetEdges_IgnoresEndpointOrder()
    {
        var graph = new UndirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("b", "a", 2));

        Assert.Equal(2, graph.GetEdges("a", "b").Count());
        Assert.Equal(2, graph.GetEdges("b", "a").Count());
    }

    [Fact]
    public void RemoveEdge_ByEndpoints_IgnoresOrientation()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.RemoveEdge("b", "a"));
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void SelfLoop_CountsTwicePerInstanceInDegree()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Equal(2, graph.Degree("a"));
    }
}
