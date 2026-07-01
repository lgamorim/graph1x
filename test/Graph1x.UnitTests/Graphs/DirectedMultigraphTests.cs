using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

public class DirectedMultigraphTests : MultigraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new DirectedMultigraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new DirectedMultigraph<string, Edge<string>>(comparer);

    [Fact]
    public void IsDirected_IsTrue()
    {
        Assert.True(CreateGraph().IsDirected);
    }

    [Fact]
    public void ContainsEdge_RespectsDirection()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.ContainsEdge("a", "b"));
        Assert.False(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void GetEdges_ReturnsAllParallelEdgesBetweenEndpoints()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("a", "c", 3));

        var edges = graph.GetEdges("a", "b").ToList();

        Assert.Equal(2, edges.Count);
        Assert.All(edges, edge => Assert.Equal("b", edge.Target));
    }

    [Fact]
    public void ParallelEdges_CountTowardOutAndInDegree()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Equal(2, graph.OutDegree("a"));
        Assert.Equal(2, graph.InDegree("b"));
        Assert.Equal(0, graph.InDegree("a"));
    }

    [Fact]
    public void SelfLoop_CountsOneInAndOneOutPerInstance()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "a"));
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Equal(2, graph.OutDegree("a"));
        Assert.Equal(2, graph.InDegree("a"));
        Assert.Equal(4, graph.Degree("a"));
    }

    [Fact]
    public void RemoveEdge_WithDistinctWeights_RemovesTheMatchingInstance()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        var cheap = new WeightedEdge<string, int>("a", "b", 1);
        var pricey = new WeightedEdge<string, int>("a", "b", 9);
        graph.AddEdge(cheap);
        graph.AddEdge(pricey);

        Assert.True(graph.RemoveEdge(pricey));
        Assert.Equal([cheap], graph.GetEdges("a", "b"));
    }
}
