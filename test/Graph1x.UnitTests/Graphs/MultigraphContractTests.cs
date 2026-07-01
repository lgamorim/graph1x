using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

/// <summary>
/// Contract tests for multigraphs: parallel edges and self-loops are allowed.
/// Inherits the direction-agnostic contract.
/// </summary>
public abstract class MultigraphContractTests : GraphContractTests
{
    [Fact]
    public void NewGraph_AllowsParallelEdges()
    {
        Assert.True(CreateGraph().AllowsParallelEdges);
    }

    [Fact]
    public void AddEdge_ParallelEdges_AllAdded()
    {
        var graph = CreateGraph();

        Assert.True(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.True(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.True(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.Equal(3, graph.EdgeCount);
        Assert.Equal(2, graph.VertexCount);
    }

    [Fact]
    public void RemoveEdge_RemovesOneParallelInstanceAtATime()
    {
        var graph = CreateGraph();
        var edge = new Edge<string>("a", "b");
        graph.AddEdge(edge);
        graph.AddEdge(edge);

        Assert.True(graph.RemoveEdge(edge));
        Assert.Equal(1, graph.EdgeCount);
        Assert.True(graph.ContainsEdge("a", "b"));

        Assert.True(graph.RemoveEdge(edge));
        Assert.Equal(0, graph.EdgeCount);
        Assert.False(graph.ContainsEdge("a", "b"));

        Assert.False(graph.RemoveEdge(edge));
    }

    [Fact]
    public void AdjacentEdges_ParallelEdges_AllYielded()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Equal(2, graph.AdjacentEdges("a").Count());
        Assert.Equal(2, graph.AdjacentEdges("b").Count());
        Assert.Equal(2, graph.Degree("a"));
    }

    [Fact]
    public void SelfLoops_MultipleAllowed()
    {
        var graph = CreateGraph();
        var loop = new Edge<string>("a", "a");

        Assert.True(graph.AddEdge(loop));
        Assert.True(graph.AddEdge(loop));
        Assert.Equal(2, graph.EdgeCount);
        Assert.Equal(4, graph.Degree("a"));
    }

    [Fact]
    public void RemoveVertex_RemovesAllParallelAndLoopEdges()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "a"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.True(graph.RemoveVertex("a"));
        Assert.Equal(1, graph.EdgeCount);
        Assert.True(graph.ContainsEdge("b", "c"));
        Assert.Equal(1, graph.Degree("b"));
    }

    [Fact]
    public void Edges_EnumeratesEveryParallelInstance()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Equal(3, graph.Edges.Count());
    }
}
