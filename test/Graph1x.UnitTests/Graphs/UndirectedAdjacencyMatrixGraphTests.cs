using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

public class UndirectedAdjacencyMatrixGraphTests : SimpleGraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new UndirectedAdjacencyMatrixGraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new UndirectedAdjacencyMatrixGraph<string, Edge<string>>(comparer);

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
    public void Edges_EnumeratesEachEdgeOnce()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "c"));

        Assert.Equal(3, graph.Edges.Count());
    }

    [Fact]
    public void RemoveVertex_MiddleOfMatrix_KeepsSymmetry()
    {
        var graph = new UndirectedAdjacencyMatrixGraph<int, Edge<int>>();
        for (var i = 0; i < 8; i++)
        {
            graph.AddEdge(new Edge<int>(i, (i + 1) % 8));
        }

        graph.RemoveVertex(3);

        Assert.Equal(7, graph.VertexCount);
        Assert.Equal(6, graph.EdgeCount);
        Assert.True(graph.ContainsEdge(5, 4));
        Assert.True(graph.ContainsEdge(0, 7));
        Assert.False(graph.ContainsEdge(2, 3));
    }

    [Fact]
    public void Algorithms_RunOnMatrixGraphs()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("x", "y"));

        Assert.Equal(2, graph.ConnectedComponents().Count);
        Assert.True(graph.IsBipartite());
        Assert.False(graph.HasCycle());
    }
}
