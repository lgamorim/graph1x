using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class BridgesAndArticulationPointsTests
{
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
    public void SingleEdge_IsABridge_WithNoArticulationPoints()
    {
        var graph = Undirected(("a", "b"));

        Assert.Equal([new Edge<string>("a", "b")], graph.FindBridges());
        Assert.Empty(graph.FindArticulationPoints());
    }

    [Fact]
    public void Path_AllEdgesAreBridges_InnerVerticesAreArticulationPoints()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"));

        Assert.Equal(3, graph.FindBridges().Count);
        Assert.True(graph.FindArticulationPoints().SetEquals(["b", "c"]));
    }

    [Fact]
    public void Cycle_HasNoBridgesAndNoArticulationPoints()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        Assert.Empty(graph.FindBridges());
        Assert.Empty(graph.FindArticulationPoints());
    }

    [Fact]
    public void TriangleWithTail_TailIsBridge_JunctionIsArticulationPoint()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"), ("c", "d"));

        Assert.Equal([new Edge<string>("c", "d")], graph.FindBridges());
        Assert.True(graph.FindArticulationPoints().SetEquals(["c"]));
    }

    [Fact]
    public void TwoTrianglesSharingAVertex_ShareVertexIsArticulationPoint_NoBridges()
    {
        var graph = Undirected(
            ("a", "b"), ("b", "m"), ("m", "a"),
            ("m", "x"), ("x", "y"), ("y", "m"));

        Assert.Empty(graph.FindBridges());
        Assert.True(graph.FindArticulationPoints().SetEquals(["m"]));
    }

    [Fact]
    public void ParallelEdges_AreNeverBridges()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal([new Edge<string>("b", "c")], graph.FindBridges());
        Assert.True(graph.FindArticulationPoints().SetEquals(["b"]));
    }

    [Fact]
    public void SelfLoops_AffectNothing()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        Assert.Equal([new Edge<string>("a", "b")], graph.FindBridges());
        Assert.Empty(graph.FindArticulationPoints());
    }

    [Fact]
    public void DisconnectedComponents_AreEachAnalyzed()
    {
        var graph = Undirected(("a", "b"), ("x", "y"), ("y", "z"));

        Assert.Equal(3, graph.FindBridges().Count);
        Assert.True(graph.FindArticulationPoints().SetEquals(["y"]));
    }

    [Fact]
    public void EmptyGraph_And_SingleVertex_YieldNothing()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.FindBridges());
        Assert.Empty(graph.FindArticulationPoints());

        graph.AddVertex("a");

        Assert.Empty(graph.FindBridges());
        Assert.Empty(graph.FindArticulationPoints());
    }

    [Fact]
    public void DirectedGraph_Throws()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.FindBridges());
        Assert.Throws<ArgumentException>(() => graph.FindArticulationPoints());
    }

    [Fact]
    public void DeepChain_DoesNotOverflowStack()
    {
        var graph = new UndirectedGraph<int, Edge<int>>();
        for (var i = 0; i < 100_000; i++)
        {
            graph.AddEdge(new Edge<int>(i, i + 1));
        }

        Assert.Equal(100_000, graph.FindBridges().Count);
        Assert.Equal(99_999, graph.FindArticulationPoints().Count);
    }

    [Fact]
    public void BridgeRemoval_ActuallyDisconnects()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"), ("c", "d"), ("d", "e"), ("e", "c"));
        graph.AddEdge(new Edge<string>("e", "f"));

        foreach (var bridge in graph.FindBridges())
        {
            var mutated = Undirected();
            foreach (var vertex in graph.Vertices)
            {
                mutated.AddVertex(vertex);
            }

            foreach (var edge in graph.Edges.Where(e => e != bridge))
            {
                mutated.AddEdge(edge);
            }

            Assert.True(mutated.ConnectedComponents().Count > graph.ConnectedComponents().Count);
        }
    }
}
