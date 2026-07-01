using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class ConnectivityTests
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

    private static void AssertComponents(
        IReadOnlyList<IReadOnlySet<string>> actual,
        params string[][] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        foreach (var component in expected)
        {
            Assert.Contains(actual, c => c.SetEquals(component));
        }
    }

    [Fact]
    public void ConnectedComponents_EmptyGraph_YieldsNone()
    {
        Assert.Empty(new UndirectedGraph<string, Edge<string>>().ConnectedComponents());
    }

    [Fact]
    public void ConnectedComponents_IsolatedVertices_EachTheirOwn()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        AssertComponents(graph.ConnectedComponents(), ["a"], ["b"]);
    }

    [Fact]
    public void ConnectedComponents_SplitsDisconnectedGraph()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("x", "y"));

        AssertComponents(graph.ConnectedComponents(), ["a", "b", "c"], ["x", "y"]);
    }

    [Fact]
    public void ConnectedComponents_SelfLoop_DoesNotAffectMembership()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        AssertComponents(graph.ConnectedComponents(), ["a", "b"]);
    }

    [Fact]
    public void ConnectedComponents_OnDirectedGraph_IgnoreDirection()
    {
        var graph = Directed(("a", "b"), ("c", "b"));

        AssertComponents(graph.ConnectedComponents(), ["a", "b", "c"]);
    }

    [Fact]
    public void IsConnected_EmptyGraph_IsTrue()
    {
        Assert.True(new UndirectedGraph<string, Edge<string>>().IsConnected());
    }

    [Fact]
    public void IsConnected_ConnectedGraph_IsTrue()
    {
        Assert.True(Undirected(("a", "b"), ("b", "c")).IsConnected());
    }

    [Fact]
    public void IsConnected_DisconnectedGraph_IsFalse()
    {
        Assert.False(Undirected(("a", "b"), ("x", "y")).IsConnected());
    }

    [Fact]
    public void WeaklyConnectedComponents_DirectedChain_IsOneComponent()
    {
        var graph = Directed(("a", "b"), ("b", "c"));

        AssertComponents(graph.WeaklyConnectedComponents(), ["a", "b", "c"]);
    }

    [Fact]
    public void WeaklyConnectedComponents_SeparatesUnlinkedParts()
    {
        var graph = Directed(("a", "b"), ("x", "y"));

        AssertComponents(graph.WeaklyConnectedComponents(), ["a", "b"], ["x", "y"]);
    }

    [Fact]
    public void Scc_EmptyGraph_YieldsNone()
    {
        Assert.Empty(new DirectedGraph<string, Edge<string>>().StronglyConnectedComponents());
    }

    [Fact]
    public void Scc_Dag_EveryVertexIsItsOwnComponent()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        Assert.Equal(4, graph.StronglyConnectedComponents().Count);
    }

    [Fact]
    public void Scc_SimpleCycle_IsOneComponent()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "a"));

        AssertComponents(graph.StronglyConnectedComponents(), ["a", "b", "c"]);
    }

    [Fact]
    public void Scc_TwoCyclesLinkedOneWay_AreSeparate()
    {
        var graph = Directed(("a", "b"), ("b", "a"), ("b", "c"), ("c", "d"), ("d", "c"));

        AssertComponents(graph.StronglyConnectedComponents(), ["a", "b"], ["c", "d"]);
    }

    [Fact]
    public void Scc_NestedCycles_MergeIntoOneComponent()
    {
        var graph = Directed(("a", "b"), ("b", "a"), ("b", "c"), ("c", "b"));

        AssertComponents(graph.StronglyConnectedComponents(), ["a", "b", "c"]);
    }

    [Fact]
    public void Scc_SelfLoop_IsItsOwnComponent()
    {
        var graph = Directed(("a", "a"), ("a", "b"));

        AssertComponents(graph.StronglyConnectedComponents(), ["a"], ["b"]);
    }

    [Fact]
    public void Scc_IsolatedVertex_IsItsOwnComponent()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("lonely");

        AssertComponents(graph.StronglyConnectedComponents(), ["lonely"]);
    }

    [Fact]
    public void Scc_ComponentsComeOutInReverseTopologicalOrder()
    {
        // a->b: b's SCC must be emitted before a's (Tarjan property).
        var graph = Directed(("a", "b"));

        var components = graph.StronglyConnectedComponents();

        Assert.Contains("b", components[0]);
        Assert.Contains("a", components[1]);
    }

    [Fact]
    public void Scc_DeepChain_DoesNotOverflowStack()
    {
        var graph = new DirectedGraph<int, Edge<int>>();
        for (var i = 0; i < 100_000; i++)
        {
            graph.AddEdge(new Edge<int>(i, i + 1));
        }

        Assert.Equal(100_001, graph.StronglyConnectedComponents().Count);
    }
}
