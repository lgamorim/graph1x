using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

/// <summary>
/// Contract tests for simple graphs: parallel edges are rejected, self-loops
/// are allowed. Inherits the direction-agnostic contract.
/// </summary>
public abstract class SimpleGraphContractTests : GraphContractTests
{
    [Fact]
    public void NewGraph_DisallowsParallelEdges()
    {
        Assert.False(CreateGraph().AllowsParallelEdges);
    }

    [Fact]
    public void AddEdge_DuplicateEndpoints_ReturnsFalse()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.False(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void AddEdge_SelfLoop_IsAllowed()
    {
        var graph = CreateGraph();

        Assert.True(graph.AddEdge(new Edge<string>("a", "a")));
        Assert.True(graph.ContainsEdge("a", "a"));
        Assert.Equal(1, graph.EdgeCount);
        Assert.Equal(2, graph.Degree("a"));
    }

    [Fact]
    public void AdjacentEdges_SelfLoop_YieldedOnce()
    {
        var graph = CreateGraph();
        var loop = new Edge<string>("a", "a");
        graph.AddEdge(loop);

        Assert.Equal([loop], graph.AdjacentEdges("a"));
    }

    [Fact]
    public void RemoveVertex_WithSelfLoop_RemovesLoop()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.True(graph.RemoveVertex("a"));
        Assert.Equal(0, graph.EdgeCount);
        Assert.Equal(0, graph.VertexCount);
    }
}
