using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests;

public class UndirectedGraphTests : SimpleGraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new UndirectedGraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new UndirectedGraph<string, Edge<string>>(comparer);

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
    public void AddEdge_ReversedEndpoints_IsDuplicate()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.False(graph.AddEdge(new Edge<string>("b", "a")));
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void Degree_CountsIncidentEdges()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "c"));

        Assert.Equal(2, graph.Degree("a"));
        Assert.Equal(1, graph.Degree("b"));
    }

    [Fact]
    public void AdjacentEdges_VisibleFromBothEndpoints()
    {
        var graph = CreateGraph();
        var edge = new Edge<string>("a", "b");
        graph.AddEdge(edge);

        Assert.Equal([edge], graph.AdjacentEdges("a"));
        Assert.Equal([edge], graph.AdjacentEdges("b"));
    }

    [Fact]
    public void RemoveEdge_ReversedEndpoints_RemovesTheEdge()
    {
        var graph = (UndirectedGraph<string, Edge<string>>)CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.RemoveEdge("b", "a"));
        Assert.Equal(0, graph.EdgeCount);
        Assert.False(graph.ContainsEdge("a", "b"));
    }

    [Fact]
    public void RemoveVertex_RemovesEdgeFromNeighborsAdjacency()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        graph.RemoveVertex("a");

        Assert.Equal(0, graph.Degree("b"));
        Assert.Empty(graph.AdjacentEdges("b"));
    }

    [Fact]
    public void Edges_EnumeratesEachEdgeOnce()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "c"));

        Assert.Equal(3, graph.Edges.Count());
    }
}
