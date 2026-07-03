using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests;

public class GraphViewTests
{
    [Fact]
    public void AsReadOnly_ResultIsNotMutable()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var view = graph.AsReadOnly();

        Assert.False(view is IMutableGraph<string, Edge<string>>);
        Assert.True(view.ContainsEdge("a", "b"));
    }

    [Fact]
    public void AsReadOnly_IsALiveView()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        var view = graph.AsReadOnly();

        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Equal(1, view.EdgeCount);
        Assert.True(view.ContainsVertex("a"));
    }

    [Fact]
    public void AsReadOnly_OnDirectedGraph_PreservesDirectedDispatch()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        IDirectedGraph<string, Edge<string>> view = graph.AsReadOnly();

        Assert.False(view is IMutableGraph<string, Edge<string>>);
        Assert.Equal(1, view.OutDegree("a"));
        Assert.Equal([new Edge<string>("a", "b")], view.OutEdges("a"));
        Assert.Equal(["a", "b", "c"], view.TopologicalSort());
    }

    [Fact]
    public void AsReadOnly_DirectedCycleDetection_UsesDirectedSemantics()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        // a->b alone is only a cycle under (incorrect) undirected dispatch.
        Assert.False(graph.AsReadOnly().HasCycle());
    }

    [Fact]
    public void AsReadOnly_IsIdempotent()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        var view = graph.AsReadOnly();

        Assert.Same(view, view.AsReadOnly());

        var directed = new DirectedGraph<string, Edge<string>>();
        var directedView = directed.AsReadOnly();

        Assert.Same(directedView, directedView.AsReadOnly());
    }

    [Fact]
    public void AsReadOnly_DelegatesTheFullQuerySurface()
    {
        var graph = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "b"));

        var view = graph.AsReadOnly();

        Assert.Equal(graph.VertexCount, view.VertexCount);
        Assert.Equal(graph.EdgeCount, view.EdgeCount);
        Assert.Equal(graph.IsDirected, view.IsDirected);
        Assert.Equal(graph.AllowsParallelEdges, view.AllowsParallelEdges);
        Assert.Same(graph.VertexComparer, view.VertexComparer);
        Assert.Equal(graph.Vertices, view.Vertices);
        Assert.Equal(graph.Edges, view.Edges);
        Assert.True(view.ContainsVertex("A"));
        Assert.True(view.ContainsEdge("B", "a"));
        Assert.Equal(3, view.Degree("b"));
        Assert.Equal(graph.AdjacentEdges("b"), view.AdjacentEdges("b"));
    }

    [Fact]
    public void ToFrozen_IsNotMutable_AndIgnoresLaterMutations()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var frozen = graph.ToFrozen();
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.RemoveVertex("a");

        Assert.False(frozen is IMutableGraph<string, Edge<string>>);
        Assert.Equal(1, frozen.EdgeCount);
        Assert.True(frozen.ContainsEdge("a", "b"));
        Assert.False(frozen.ContainsVertex("c"));
    }

    [Fact]
    public void ToFrozen_OnDirectedGraph_KeepsDirectionAndDispatch()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        IDirectedGraph<string, Edge<string>> frozen = graph.ToFrozen();

        Assert.True(frozen.ContainsEdge("a", "b"));
        Assert.False(frozen.ContainsEdge("b", "a"));
        Assert.Equal(1, frozen.InDegree("b"));
    }

    [Fact]
    public void ToFrozen_PreservesParallelEdges()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var frozen = graph.ToFrozen();

        Assert.Equal(2, frozen.EdgeCount);
        Assert.True(frozen.AllowsParallelEdges);
    }

    [Fact]
    public void ToFrozen_PreservesComparer()
    {
        var graph = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("Lisbon", "Porto"));

        var frozen = graph.ToFrozen();

        Assert.True(frozen.ContainsEdge("PORTO", "lisbon"));
    }

    [Fact]
    public void ToFrozen_PreservesIsolatedVerticesAndSelfLoops()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "a"));
        graph.AddVertex("lonely");

        var frozen = graph.ToFrozen();

        Assert.Equal(2, frozen.VertexCount);
        Assert.Equal(2, frozen.Degree("a"));
        Assert.True(frozen.ContainsVertex("lonely"));
    }

    [Fact]
    public void Algorithms_RunOnFrozenGraphs()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 3));

        var frozen = graph.ToFrozen();

        Assert.Equal(5, frozen.ShortestPath("a", "c").Distance);
        Assert.Single(frozen.ConnectedComponents());
    }

    [Fact]
    public void NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ((IReadOnlyGraph<string, Edge<string>>)null!).AsReadOnly());
        Assert.Throws<ArgumentNullException>(
            () => ((IReadOnlyGraph<string, Edge<string>>)null!).ToFrozen());
    }
}
