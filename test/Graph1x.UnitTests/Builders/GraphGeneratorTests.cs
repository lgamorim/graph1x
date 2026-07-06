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

    [Fact]
    public void BarabasiAlbert_HasTheClosedFormEdgeCount()
    {
        // m initial edgeless vertices, then every new vertex brings m edges.
        var graph = GraphGenerator.BarabasiAlbert(50, edgesPerNewVertex: 3, seed: 42);

        Assert.Equal(50, graph.VertexCount);
        Assert.Equal(3 * (50 - 3), graph.EdgeCount);
    }

    [Fact]
    public void BarabasiAlbert_IsConnected()
    {
        var graph = GraphGenerator.BarabasiAlbert(40, edgesPerNewVertex: 2, seed: 7);

        Assert.True(graph.IsConnected());
    }

    [Fact]
    public void BarabasiAlbert_HasNoSelfLoopsOrParallelEdges()
    {
        var graph = GraphGenerator.BarabasiAlbert(60, edgesPerNewVertex: 4, seed: 11);

        Assert.All(graph.Edges, edge => Assert.NotEqual(edge.Source, edge.Target));
        Assert.False(graph.AllowsParallelEdges); // simple graph enforces the rest
    }

    [Fact]
    public void BarabasiAlbert_SameSeed_IsDeterministic()
    {
        var first = GraphGenerator.BarabasiAlbert(30, edgesPerNewVertex: 2, seed: 42);
        var second = GraphGenerator.BarabasiAlbert(30, edgesPerNewVertex: 2, seed: 42);

        Assert.Equal(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void BarabasiAlbert_DifferentSeeds_Differ()
    {
        var first = GraphGenerator.BarabasiAlbert(30, edgesPerNewVertex: 2, seed: 42);
        var second = GraphGenerator.BarabasiAlbert(30, edgesPerNewVertex: 2, seed: 43);

        Assert.NotEqual(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void BarabasiAlbert_MinimalSize_IsASingleAttachment()
    {
        // n = m + 1: the one new vertex attaches to all m initial vertices.
        var graph = GraphGenerator.BarabasiAlbert(4, edgesPerNewVertex: 3, seed: 1);

        Assert.Equal(3, graph.EdgeCount);
        Assert.Equal(3, graph.Degree(3));
    }

    [Fact]
    public void BarabasiAlbert_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.BarabasiAlbert(10, 0, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.BarabasiAlbert(10, -1, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.BarabasiAlbert(10, 10, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphGenerator.BarabasiAlbert(10, 11, seed: 1));
    }

    [Fact]
    public void WattsStrogatz_WithoutRewiring_IsTheExactRingLattice()
    {
        var graph = GraphGenerator.WattsStrogatz(10, nearestNeighbors: 4, rewiringProbability: 0.0, seed: 1);

        Assert.Equal(10, graph.VertexCount);
        Assert.Equal(10 * 4 / 2, graph.EdgeCount);
        Assert.All(graph.Vertices, vertex => Assert.Equal(4, graph.Degree(vertex)));
        Assert.True(graph.ContainsEdge(0, 1));
        Assert.True(graph.ContainsEdge(0, 2));
        Assert.True(graph.ContainsEdge(0, 9));
        Assert.True(graph.ContainsEdge(0, 8));
        Assert.False(graph.ContainsEdge(0, 3));
    }

    [Fact]
    public void WattsStrogatz_FullRewiring_KeepsTheEdgeCount()
    {
        var graph = GraphGenerator.WattsStrogatz(20, nearestNeighbors: 4, rewiringProbability: 1.0, seed: 42);

        Assert.Equal(20 * 4 / 2, graph.EdgeCount);
    }

    [Fact]
    public void WattsStrogatz_RewiringNeverCreatesSelfLoopsOrParallelEdges()
    {
        var graph = GraphGenerator.WattsStrogatz(25, nearestNeighbors: 6, rewiringProbability: 0.7, seed: 3);

        Assert.All(graph.Edges, edge => Assert.NotEqual(edge.Source, edge.Target));
        Assert.Equal(25 * 6 / 2, graph.EdgeCount); // duplicates would have collapsed
    }

    [Fact]
    public void WattsStrogatz_SameSeed_IsDeterministic()
    {
        var first = GraphGenerator.WattsStrogatz(30, 4, 0.5, seed: 42);
        var second = GraphGenerator.WattsStrogatz(30, 4, 0.5, seed: 42);

        Assert.Equal(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void WattsStrogatz_DifferentSeeds_Differ()
    {
        var first = GraphGenerator.WattsStrogatz(30, 4, 0.5, seed: 42);
        var second = GraphGenerator.WattsStrogatz(30, 4, 0.5, seed: 43);

        Assert.NotEqual(first.Edges.ToHashSet(), second.Edges.ToHashSet());
    }

    [Fact]
    public void WattsStrogatz_ZeroNeighbors_IsEdgeless()
    {
        var graph = GraphGenerator.WattsStrogatz(5, nearestNeighbors: 0, rewiringProbability: 0.5, seed: 1);

        Assert.Equal(5, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void WattsStrogatz_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(
            () => GraphGenerator.WattsStrogatz(10, nearestNeighbors: 3, rewiringProbability: 0.5, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphGenerator.WattsStrogatz(10, nearestNeighbors: 10, rewiringProbability: 0.5, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphGenerator.WattsStrogatz(10, nearestNeighbors: -2, rewiringProbability: 0.5, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphGenerator.WattsStrogatz(10, nearestNeighbors: 4, rewiringProbability: -0.1, seed: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GraphGenerator.WattsStrogatz(10, nearestNeighbors: 4, rewiringProbability: 1.1, seed: 1));
    }
}
