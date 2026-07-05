using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class DistanceMetricsTests
{
    [Fact]
    public void PathGraph_HasKnownMetrics()
    {
        var graph = GraphGenerator.Path(4); // 0-1-2-3

        Assert.Equal(3, graph.Diameter());
        Assert.Equal(2, graph.Radius());
        Assert.Equal(3, graph.Eccentricity(0));
        Assert.Equal(2, graph.Eccentricity(1));
        Assert.True(graph.Center().SetEquals([1, 2]));
        Assert.True(graph.Periphery().SetEquals([0, 3]));
    }

    [Fact]
    public void CycleGraph_EveryVertexIsCentral()
    {
        var graph = GraphGenerator.Cycle(5);

        Assert.Equal(2, graph.Diameter());
        Assert.Equal(2, graph.Radius());
        Assert.Equal(5, graph.Center().Count);
        Assert.Equal(5, graph.Periphery().Count);
    }

    [Fact]
    public void StarGraph_CenterIsTheHub()
    {
        var graph = GraphGenerator.Star(4);

        Assert.Equal(1, graph.Radius());
        Assert.Equal(2, graph.Diameter());
        Assert.True(graph.Center().SetEquals([0]));
        Assert.Equal(4, graph.Periphery().Count);
    }

    [Fact]
    public void CompleteGraph_DiameterIsOne()
    {
        var graph = GraphGenerator.Complete(4);

        Assert.Equal(1, graph.Diameter());
        Assert.Equal(1.0, graph.AveragePathLength());
    }

    [Fact]
    public void Grid_DiameterIsManhattanCornerDistance()
    {
        Assert.Equal(4, GraphGenerator.Grid(3, 3).Diameter());
    }

    [Fact]
    public void AveragePathLength_PathGraph_IsExact()
    {
        var graph = GraphGenerator.Path(3); // ordered-pair distances: 1,2,1,1,2,1 -> 8/6

        Assert.Equal(4.0 / 3.0, graph.AveragePathLength(), precision: 12);
    }

    [Fact]
    public void WeightedMetrics_UseTheSelector()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 3));

        Assert.Equal(5, graph.Diameter(e => e.Weight));
        Assert.Equal(3, graph.Radius(e => e.Weight));
        Assert.True(graph.Center(e => e.Weight).SetEquals(["b"]));
        Assert.Equal(5, graph.Eccentricity("a", e => e.Weight));
    }

    [Fact]
    public void DirectedCycle_UsesDirectedDistances()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "a"));

        Assert.Equal(2, graph.Diameter()); // a->c takes two hops; c->a takes one
        Assert.Equal(2, graph.Eccentricity("a"));
    }

    [Fact]
    public void DirectedChain_IsNotStronglyConnected_Throws()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<InvalidOperationException>(() => graph.Diameter());
        Assert.Throws<InvalidOperationException>(() => graph.Eccentricity("a"));
    }

    [Fact]
    public void DisconnectedGraph_Throws()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddVertex("island");

        Assert.Throws<InvalidOperationException>(() => graph.Diameter());
        Assert.Throws<InvalidOperationException>(() => graph.Radius());
        Assert.Throws<InvalidOperationException>(() => graph.Center());
        Assert.Throws<InvalidOperationException>(() => graph.AveragePathLength());
    }

    [Fact]
    public void EmptyGraph_Throws()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Throws<InvalidOperationException>(() => graph.Diameter());
    }

    [Fact]
    public void SingleVertex_HasZeroMetrics()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Equal(0, graph.Diameter());
        Assert.Equal(0, graph.Radius());
        Assert.True(graph.Center().SetEquals(["a"]));
        Assert.Equal(0.0, graph.AveragePathLength());
    }

    [Fact]
    public void NegativeWeights_Throw()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", -1));

        Assert.Throws<NegativeWeightException>(() => graph.Diameter(e => e.Weight));
    }

    [Fact]
    public void Eccentricity_MissingVertex_Throws()
    {
        var graph = GraphGenerator.Path(3);

        Assert.Throws<ArgumentException>(() => graph.Eccentricity(99));
    }
}
