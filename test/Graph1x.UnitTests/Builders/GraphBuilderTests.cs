using Graph1x;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Builders;

public class GraphBuilderTests
{
    [Fact]
    public void Directed_BuildsTypedDirectedGraph()
    {
        DirectedGraph<string, Edge<string>> graph = Graph.Directed<string>()
            .AddEdge("a", "b")
            .AddEdge("b", "c")
            .Build();

        Assert.True(graph.ContainsEdge("a", "b"));
        Assert.Equal(3, graph.VertexCount);
    }

    [Fact]
    public void Undirected_BuildsTypedUndirectedGraph()
    {
        UndirectedGraph<string, Edge<string>> graph = Graph.Undirected<string>()
            .AddEdge("a", "b")
            .Build();

        Assert.True(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void DirectedWeighted_AddsWeightedEdgesInline()
    {
        var graph = Graph.DirectedWeighted<string, int>()
            .AddEdge("a", "b", 3)
            .AddEdge("b", "c", 4)
            .Build();

        Assert.Equal(3, graph.OutEdges("a").Single().Weight);
        Assert.Equal(2, graph.EdgeCount);
    }

    [Fact]
    public void UndirectedWeighted_AddsWeightedEdgesInline()
    {
        var graph = Graph.UndirectedWeighted<string, double>()
            .AddEdge("a", "b", 1.5)
            .Build();

        Assert.True(graph.ContainsEdge("b", "a"));
    }

    [Fact]
    public void AddVertices_AddsAllAtOnce()
    {
        var graph = Graph.Directed<int>()
            .AddVertices(1, 2, 3)
            .Build();

        Assert.Equal(3, graph.VertexCount);
    }

    [Fact]
    public void AddEdges_AddsAllAtOnce()
    {
        var graph = Graph.Directed<string>()
            .AddEdges(new Edge<string>("a", "b"), new Edge<string>("b", "c"))
            .Build();

        Assert.Equal(2, graph.EdgeCount);
    }

    [Fact]
    public void Wrap_SeedsBuilderWithAnyMutableGraph()
    {
        var dag = new DirectedAcyclicGraph<string, Edge<string>>();

        var built = Graph.Wrap(dag)
            .AddEdge(new Edge<string>("a", "b"))
            .Build();

        Assert.Same(dag, built);
        Assert.True(built.ContainsEdge("a", "b"));
    }

    [Fact]
    public void Builder_IsFluent_ReturnsSameBuilderInstance()
    {
        var builder = Graph.Directed<string>();

        Assert.Same(builder, builder.AddVertex("a"));
    }

    [Fact]
    public void Wrap_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Graph.Wrap<int, Edge<int>>(null!));
    }
}
