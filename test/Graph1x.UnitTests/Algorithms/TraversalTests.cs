using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class TraversalTests
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

    private static UndirectedGraph<string, Edge<string>> Undirected(params (string, string)[] edges)
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    [Fact]
    public void Bfs_VisitsInBreadthOrder()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "e"));

        Assert.Equal(["a", "b", "c", "d", "e"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_SingleVertex_YieldsJustTheStart()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Equal(["a"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_MissingStart_ThrowsEagerly()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentException>(() => graph.BreadthFirstSearch("ghost"));
    }

    [Fact]
    public void Bfs_NullStart_Throws()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentNullException>(() => graph.BreadthFirstSearch(null!));
    }

    [Fact]
    public void Bfs_WithCycle_VisitsEachVertexOnce()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "a"));

        Assert.Equal(["a", "b", "c"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_DisconnectedComponent_IsNotVisited()
    {
        var graph = Directed(("a", "b"), ("x", "y"));

        Assert.Equal(["a", "b"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_Directed_FollowsEdgeDirectionOnly()
    {
        var graph = Directed(("b", "a"), ("a", "c"));

        Assert.Equal(["a", "c"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_Undirected_TraversesBothWays()
    {
        var graph = Undirected(("b", "a"), ("a", "c"));

        Assert.Equal(["a", "b", "c"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_SelfLoop_DoesNotRepeatVertex()
    {
        var graph = Directed(("a", "a"), ("a", "b"));

        Assert.Equal(["a", "b"], graph.BreadthFirstSearch("a"));
    }

    [Fact]
    public void Bfs_IsDeferred_SeesMutationsBeforeEnumeration()
    {
        var graph = Directed(("a", "b"));
        var traversal = graph.BreadthFirstSearch("a");
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal(["a", "b", "c"], traversal);
    }

    [Fact]
    public void Dfs_VisitsInPreOrder()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("a", "d"));

        Assert.Equal(["a", "b", "c", "d"], graph.DepthFirstSearch("a"));
    }

    [Fact]
    public void Dfs_PostOrder_YieldsChildrenBeforeParents()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("a", "d"));

        Assert.Equal(["c", "b", "d", "a"], graph.DepthFirstSearchPostOrder("a"));
    }

    [Fact]
    public void Dfs_WithCycle_VisitsEachVertexOnce()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        Assert.Equal(["a", "b"], graph.DepthFirstSearch("a"));
    }

    [Fact]
    public void Dfs_MissingStart_ThrowsEagerly()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentException>(() => graph.DepthFirstSearch("ghost"));
    }

    [Fact]
    public void Dfs_Undirected_DoesNotBounceBackToParent()
    {
        var graph = Undirected(("a", "b"), ("b", "c"));

        Assert.Equal(["a", "b", "c"], graph.DepthFirstSearch("a"));
    }

    [Fact]
    public void Dfs_DeepChain_DoesNotOverflowStack()
    {
        var graph = new DirectedGraph<int, Edge<int>>();
        for (var i = 0; i < 100_000; i++)
        {
            graph.AddEdge(new Edge<int>(i, i + 1));
        }

        Assert.Equal(100_001, graph.DepthFirstSearch(0).Count());
        Assert.Equal(100_001, graph.DepthFirstSearchPostOrder(0).Count());
    }
}
