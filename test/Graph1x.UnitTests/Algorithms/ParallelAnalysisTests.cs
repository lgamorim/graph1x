using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class ParallelAnalysisTests
{
    private const int Precision = 6;

    private static ParallelOptions Parallel(int degreeOfParallelism = -1) => new()
    {
        MaxDegreeOfParallelism = degreeOfParallelism,
    };

    private static int DeterministicWeight(Edge<int> edge) => ((edge.Source + edge.Target) % 5) + 1;

    [Fact]
    public void BetweennessCentrality_Parallel_MatchesTheSequentialReference()
    {
        var graph = GraphGenerator.BarabasiAlbert(60, edgesPerNewVertex: 2, seed: 42);

        var sequential = graph.BetweennessCentrality();
        var parallel = graph.BetweennessCentrality(Parallel());

        Assert.Equal(sequential.Count, parallel.Count);
        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex], Precision);
        }
    }

    [Fact]
    public void BetweennessCentrality_Weighted_Parallel_MatchesTheSequentialReference()
    {
        var graph = GraphGenerator.WattsStrogatz(50, nearestNeighbors: 4, rewiringProbability: 0.3, seed: 7);

        var sequential = graph.BetweennessCentrality(DeterministicWeight);
        var parallel = graph.BetweennessCentrality(DeterministicWeight, Parallel());

        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex], Precision);
        }
    }

    [Fact]
    public void BetweennessCentrality_SingleDegreeOfParallelism_MatchesSequentialExactly()
    {
        var graph = GraphGenerator.ErdosRenyi(40, 0.15, seed: 5);

        var sequential = graph.BetweennessCentrality();
        var parallel = graph.BetweennessCentrality(Parallel(degreeOfParallelism: 1));

        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex]);
        }
    }

    [Fact]
    public void BetweennessCentrality_Parallel_OnDirectedGraphs_MatchesSequential()
    {
        var graph = GraphGenerator.ErdosRenyiDirected(40, 0.15, seed: 11);

        var sequential = graph.BetweennessCentrality();
        var parallel = graph.BetweennessCentrality(Parallel());

        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex], Precision);
        }
    }

    [Fact]
    public void ClosenessCentrality_Parallel_MatchesTheSequentialReferenceExactly()
    {
        var graph = GraphGenerator.BarabasiAlbert(50, edgesPerNewVertex: 3, seed: 9);

        var sequential = graph.ClosenessCentrality();
        var parallel = graph.ClosenessCentrality(Parallel());

        Assert.Equal(sequential.Count, parallel.Count);
        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex]); // per-vertex results are independent
        }
    }

    [Fact]
    public void ClosenessCentrality_Weighted_Parallel_MatchesSequentialOnDisconnectedInput()
    {
        var graph = GraphGenerator.ErdosRenyi(30, 0.05, seed: 3); // sparse: almost surely disconnected

        var sequential = graph.ClosenessCentrality(DeterministicWeight);
        var parallel = graph.ClosenessCentrality(DeterministicWeight, Parallel());

        foreach (var (vertex, expected) in sequential)
        {
            Assert.Equal(expected, parallel[vertex]);
        }
    }

    [Fact]
    public void DistanceMetrics_Parallel_MatchTheSequentialReference()
    {
        var graph = GraphGenerator.WattsStrogatz(40, nearestNeighbors: 4, rewiringProbability: 0.2, seed: 21);

        Assert.Equal(graph.Diameter(), graph.Diameter(Parallel()));
        Assert.Equal(graph.Radius(), graph.Radius(Parallel()));
        Assert.Equal(graph.Center(), graph.Center(Parallel()));
        Assert.Equal(graph.Periphery(), graph.Periphery(Parallel()));
        Assert.Equal(graph.AveragePathLength(), graph.AveragePathLength(Parallel()), Precision);
    }

    [Fact]
    public void DistanceMetrics_Weighted_Parallel_MatchTheSequentialReference()
    {
        var graph = GraphGenerator.Cycle(30);

        Assert.Equal(graph.Diameter(DeterministicWeight), graph.Diameter(DeterministicWeight, Parallel()));
        Assert.Equal(graph.Radius(DeterministicWeight), graph.Radius(DeterministicWeight, Parallel()));
        Assert.Equal(graph.Center(DeterministicWeight), graph.Center(DeterministicWeight, Parallel()));
        Assert.Equal(graph.Periphery(DeterministicWeight), graph.Periphery(DeterministicWeight, Parallel()));
        Assert.Equal(
            graph.AveragePathLength(DeterministicWeight),
            graph.AveragePathLength(DeterministicWeight, Parallel()),
            Precision);
    }

    [Fact]
    public void DistanceMetrics_Parallel_RejectDisconnectedGraphs()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddVertex("island");

        Assert.Throws<InvalidOperationException>(() => graph.Diameter(Parallel()));
        Assert.Throws<InvalidOperationException>(() => graph.AveragePathLength(Parallel()));
    }

    [Fact]
    public void ParallelOverloads_OnEmptyGraphs_MatchSequentialBehavior()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.BetweennessCentrality(Parallel()));
        Assert.Empty(graph.ClosenessCentrality(Parallel()));
        Assert.Throws<InvalidOperationException>(() => graph.Diameter(Parallel()));
    }

    [Fact]
    public void ParallelOverloads_OnASingleVertex_MatchSequentialBehavior()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Equal(0.0, graph.BetweennessCentrality(Parallel())["a"]);
        Assert.Equal(0.0, graph.ClosenessCentrality(Parallel())["a"]);
        Assert.Equal(0, graph.Diameter(Parallel()));
    }

    [Fact]
    public void ParallelOverloads_ObserveTheCancellationToken()
    {
        var graph = GraphGenerator.Complete(20);
        using var source = new CancellationTokenSource();
        source.Cancel();
        var options = new ParallelOptions { CancellationToken = source.Token };

        Assert.Throws<OperationCanceledException>(() => graph.BetweennessCentrality(options));
        Assert.Throws<OperationCanceledException>(() => graph.ClosenessCentrality(options));
        Assert.Throws<OperationCanceledException>(() => graph.Diameter(options));
        Assert.Throws<OperationCanceledException>(() => graph.AveragePathLength(options));
    }

    [Fact]
    public void ParallelOverloads_NullArguments_Throw()
    {
        var graph = GraphGenerator.Complete(3);

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<int, Edge<int>>)!.BetweennessCentrality(Parallel()));
        Assert.Throws<ArgumentNullException>(() => graph.BetweennessCentrality(default(ParallelOptions)!));
        Assert.Throws<ArgumentNullException>(() => graph.ClosenessCentrality(default(ParallelOptions)!));
        Assert.Throws<ArgumentNullException>(() => graph.Diameter(default(ParallelOptions)!));
    }
}
