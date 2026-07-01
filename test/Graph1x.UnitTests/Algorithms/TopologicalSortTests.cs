using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class TopologicalSortTests
{
    private static DirectedGraph<string, Edge<string>> Directed(params (string, string)[] edges)
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    private static void AssertRespectsEdges<TEdge>(IDirectedGraph<string, TEdge> graph, IReadOnlyList<string> order)
        where TEdge : Graph1x.Edges.IEdge<string>
    {
        var position = order.Select((vertex, index) => (vertex, index)).ToDictionary(p => p.vertex, p => p.index);
        Assert.Equal(graph.VertexCount, order.Count);
        foreach (var edge in graph.Edges)
        {
            Assert.True(
                position[edge.Source] < position[edge.Target],
                $"{edge.Source} must sort before {edge.Target}");
        }
    }

    [Fact]
    public void EmptyGraph_YieldsEmptyOrder()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.TopologicalSort());
    }

    [Fact]
    public void SingleVertex_YieldsThatVertex()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Equal(["a"], graph.TopologicalSort());
    }

    [Fact]
    public void Chain_SortsInChainOrder()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "d"));

        Assert.Equal(["a", "b", "c", "d"], graph.TopologicalSort());
    }

    [Fact]
    public void Diamond_RespectsAllEdges()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        var order = graph.TopologicalSort();

        AssertRespectsEdges(graph, order);
        Assert.Equal(["a", "b", "c", "d"], order); // Kahn FIFO with insertion-order tie handling
    }

    [Fact]
    public void DisconnectedDag_IncludesAllVertices()
    {
        var graph = Directed(("a", "b"), ("x", "y"));
        graph.AddVertex("lonely");

        var order = graph.TopologicalSort();

        AssertRespectsEdges(graph, order);
        Assert.Contains("lonely", order);
    }

    [Fact]
    public void CyclicGraph_ThrowsGraphCycleException()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "a"));

        var exception = Assert.Throws<GraphCycleException>(() => graph.TopologicalSort());

        Assert.NotEmpty(exception.Cycle);
        Assert.Contains("a", exception.Cycle.Cast<string>());
    }

    [Fact]
    public void SelfLoop_ThrowsGraphCycleException()
    {
        var graph = Directed(("a", "a"));

        Assert.Throws<GraphCycleException>(() => graph.TopologicalSort());
    }

    [Fact]
    public void Multigraph_ParallelEdges_StillSortable()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal(["a", "b", "c"], graph.TopologicalSort());
    }

    [Fact]
    public void GraphCycleException_HasStandardConstructors()
    {
        Assert.NotNull(new GraphCycleException());
        Assert.Equal("boom", new GraphCycleException("boom").Message);

        var inner = new InvalidOperationException();
        var exception = new GraphCycleException("boom", inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Empty(exception.Cycle);
    }
}
