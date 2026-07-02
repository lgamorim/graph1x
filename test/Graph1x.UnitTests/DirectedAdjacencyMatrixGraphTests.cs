using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests;

public class DirectedAdjacencyMatrixGraphTests : SimpleGraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new DirectedAdjacencyMatrixGraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new DirectedAdjacencyMatrixGraph<string, Edge<string>>(comparer);

    private static DirectedAdjacencyMatrixGraph<string, Edge<string>> CreateMatrix() => new();

    [Fact]
    public void IsDirected_IsTrue()
    {
        Assert.True(CreateGraph().IsDirected);
    }

    [Fact]
    public void ContainsEdge_RespectsDirection()
    {
        var graph = CreateMatrix();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.ContainsEdge("a", "b"));
        Assert.False(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void OutEdges_And_InEdges_Work()
    {
        var graph = CreateMatrix();
        var ab = new Edge<string>("a", "b");
        var ca = new Edge<string>("c", "a");
        graph.AddEdge(ab);
        graph.AddEdge(ca);

        Assert.Equal([ab], graph.OutEdges("a"));
        Assert.Equal([ca], graph.InEdges("a"));
        Assert.Equal(1, graph.OutDegree("a"));
        Assert.Equal(1, graph.InDegree("a"));
    }

    [Fact]
    public void Growth_BeyondInitialCapacity_KeepsAllData()
    {
        var graph = new DirectedAdjacencyMatrixGraph<int, Edge<int>>();
        for (var i = 0; i < 50; i++)
        {
            graph.AddEdge(new Edge<int>(i, i + 1));
        }

        Assert.Equal(51, graph.VertexCount);
        Assert.Equal(50, graph.EdgeCount);
        for (var i = 0; i < 50; i++)
        {
            Assert.True(graph.ContainsEdge(i, i + 1));
        }
    }

    [Fact]
    public void RemoveVertex_MiddleOfMatrix_KeepsRemainingEdgesIntact()
    {
        var graph = new DirectedAdjacencyMatrixGraph<int, Edge<int>>();
        for (var i = 0; i < 10; i++)
        {
            graph.AddEdge(new Edge<int>(i, (i + 1) % 10));
        }

        graph.RemoveVertex(4);

        Assert.Equal(9, graph.VertexCount);
        Assert.Equal(8, graph.EdgeCount);
        Assert.True(graph.ContainsEdge(9, 0));
        Assert.True(graph.ContainsEdge(5, 6));
        Assert.False(graph.ContainsEdge(3, 4));
        Assert.False(graph.ContainsEdge(4, 5));
    }

    [Fact]
    public void Algorithms_RunOnMatrixGraphs()
    {
        var graph = CreateMatrix();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal(["a", "b", "c"], graph.TopologicalSort());
        Assert.Equal(["a", "b", "c"], graph.BreadthFirstSearch("a"));
        Assert.False(graph.HasCycle());
    }
}
