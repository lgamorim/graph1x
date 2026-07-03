using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Builders;

public class GraphGeneratorTests
{
    [Fact]
    public void Complete_HasEveryPair()
    {
        var graph = GraphGenerator.Complete(5);

        Assert.Equal(5, graph.VertexCount);
        Assert.Equal(10, graph.EdgeCount);
        Assert.Equal(1.0, graph.Density());
    }

    [Fact]
    public void Complete_TrivialSizes()
    {
        Assert.Equal(0, GraphGenerator.Complete(0).VertexCount);
        Assert.Equal(1, GraphGenerator.Complete(1).VertexCount);
        Assert.Equal(0, GraphGenerator.Complete(1).EdgeCount);
    }

    [Fact]
    public void CompleteBipartite_IsBipartiteWithAllCrossEdges()
    {
        var graph = GraphGenerator.CompleteBipartite(2, 3);

        Assert.Equal(5, graph.VertexCount);
        Assert.Equal(6, graph.EdgeCount);
        Assert.True(graph.IsBipartite());
    }

    [Fact]
    public void Path_IsAnAcyclicChainOfBridges()
    {
        var graph = GraphGenerator.Path(4);

        Assert.Equal(4, graph.VertexCount);
        Assert.Equal(3, graph.EdgeCount);
        Assert.False(graph.HasCycle());
        Assert.Equal(3, graph.FindBridges().Count);
    }

    [Fact]
    public void Path_TrivialSizes()
    {
        Assert.Equal(0, GraphGenerator.Path(0).VertexCount);
        Assert.Equal(1, GraphGenerator.Path(1).VertexCount);
        Assert.Equal(0, GraphGenerator.Path(1).EdgeCount);
    }

    [Fact]
    public void Cycle_EveryVertexHasDegreeTwo_AndAnEulerianCircuitExists()
    {
        var graph = GraphGenerator.Cycle(5);

        Assert.Equal(5, graph.EdgeCount);
        Assert.True(graph.HasCycle());
        Assert.True(graph.HasEulerianCircuit());
        Assert.All(graph.Vertices, vertex => Assert.Equal(2, graph.Degree(vertex)));
    }

    [Fact]
    public void Cycle_RequiresAtLeastThreeVertices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.Cycle(2));
    }

    [Fact]
    public void Star_CenterIsTheArticulationPoint()
    {
        var graph = GraphGenerator.Star(4);

        Assert.Equal(5, graph.VertexCount);
        Assert.Equal(4, graph.EdgeCount);
        Assert.Equal(4, graph.Degree(0));
        Assert.True(graph.FindArticulationPoints().SetEquals([0]));
        Assert.True(graph.IsBipartite());
    }

    [Fact]
    public void Grid_IsConnectedAndBipartite_WithKnownEdgeCount()
    {
        var graph = GraphGenerator.Grid(3, 3);

        Assert.Equal(9, graph.VertexCount);
        Assert.Equal(12, graph.EdgeCount); // 2wh - w - h
        Assert.True(graph.IsConnected());
        Assert.True(graph.IsBipartite());
    }

    [Fact]
    public void Grid_WithAZeroDimension_IsEmpty()
    {
        Assert.Equal(0, GraphGenerator.Grid(0, 5).VertexCount);
        Assert.Equal(0, GraphGenerator.Grid(5, 0).VertexCount);
    }

    [Fact]
    public void ErdosRenyi_SameSeed_IsDeterministic()
    {
        var first = GraphGenerator.ErdosRenyi(20, 0.3, seed: 42);
        var second = GraphGenerator.ErdosRenyi(20, 0.3, seed: 42);

        Assert.Equal(first.EdgeCount, second.EdgeCount);
        Assert.Equal(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void ErdosRenyi_DifferentSeeds_Differ()
    {
        var first = GraphGenerator.ErdosRenyi(20, 0.3, seed: 42);
        var second = GraphGenerator.ErdosRenyi(20, 0.3, seed: 43);

        Assert.NotEqual(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void ErdosRenyi_ProbabilityExtremes()
    {
        Assert.Equal(0, GraphGenerator.ErdosRenyi(10, 0.0, seed: 1).EdgeCount);
        Assert.Equal(45, GraphGenerator.ErdosRenyi(10, 1.0, seed: 1).EdgeCount);
    }

    [Fact]
    public void ErdosRenyiDirected_ProbabilityOne_HasAllOrderedPairs()
    {
        var graph = GraphGenerator.ErdosRenyiDirected(6, 1.0, seed: 1);

        Assert.Equal(30, graph.EdgeCount); // n(n-1), no self-loops
        Assert.True(graph.IsDirected);
    }

    [Fact]
    public void ErdosRenyi_HasNoSelfLoops()
    {
        var graph = GraphGenerator.ErdosRenyi(15, 1.0, seed: 7);

        Assert.All(graph.Edges, edge => Assert.NotEqual(edge.Source, edge.Target));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.Complete(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.ErdosRenyi(5, -0.1, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.ErdosRenyi(5, 1.1, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.CompleteBipartite(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.Star(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.Grid(-1, 2));
    }
}
