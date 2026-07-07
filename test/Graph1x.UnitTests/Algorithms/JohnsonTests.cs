using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class JohnsonTests
{
    private static DirectedGraph<string, WeightedEdge<string, int>> Directed(
        params (string Source, string Target, int Weight)[] edges)
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, weight) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, weight));
        }

        return graph;
    }

    private static JohnsonAllShortestPaths<TVertex, WeightedEdge<TVertex, int>, int> Johnson<TVertex>()
        where TVertex : notnull
        => new(edge => edge.Weight);

    [Fact]
    public void Compute_MatchesFloydWarshall_OnRandomDirectedGraphs()
    {
        foreach (var seed in new[] { 3, 17, 42 })
        {
            var random = GraphGenerator.ErdosRenyiDirected(30, 0.1, seed);
            var graph = new DirectedGraph<int, WeightedEdge<int, int>>();
            foreach (var vertex in random.Vertices)
            {
                graph.AddVertex(vertex);
            }

            foreach (var edge in random.Edges)
            {
                graph.AddEdge(new WeightedEdge<int, int>(
                    edge.Source, edge.Target, (edge.Source * 31 + edge.Target) % 10 + 1));
            }

            var johnson = new JohnsonAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
                .Compute(graph);
            var floydWarshall = new FloydWarshallAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
                .Compute(graph);

            foreach (var source in graph.Vertices)
            {
                foreach (var target in graph.Vertices)
                {
                    var expected = floydWarshall.Between(source, target);
                    var actual = johnson.Between(source, target);
                    Assert.Equal(expected.IsReachable, actual.IsReachable);
                    if (expected.IsReachable)
                    {
                        Assert.Equal(expected.Distance, actual.Distance);
                    }
                }
            }
        }
    }

    [Fact]
    public void Compute_NegativeWeights_MatchFloydWarshall()
    {
        var graph = Directed(
            ("a", "b", -2),
            ("b", "c", -1),
            ("c", "a", 4),
            ("c", "x", 2),
            ("c", "y", -3),
            ("z", "x", 1),
            ("z", "y", -4));

        var johnson = Johnson<string>().Compute(graph);
        var floydWarshall = new FloydWarshallAllShortestPaths<string, WeightedEdge<string, int>, int>(e => e.Weight)
            .Compute(graph);

        foreach (var source in graph.Vertices)
        {
            foreach (var target in graph.Vertices)
            {
                var expected = floydWarshall.Between(source, target);
                var actual = johnson.Between(source, target);
                Assert.Equal(expected.IsReachable, actual.IsReachable);
                if (expected.IsReachable)
                {
                    Assert.Equal(expected.Distance, actual.Distance);
                }
            }
        }
    }

    [Fact]
    public void Compute_NegativeCycle_ThrowsNegativeCycleException()
    {
        var graph = Directed(("a", "b", 1), ("b", "a", -2));

        Assert.Throws<NegativeCycleException>(() => Johnson<string>().Compute(graph));
    }

    [Fact]
    public void Compute_NegativeSelfLoop_ThrowsNegativeCycleException()
    {
        var graph = Directed(("a", "a", -1), ("a", "b", 1));

        Assert.Throws<NegativeCycleException>(() => Johnson<string>().Compute(graph));
    }

    [Fact]
    public void Compute_UndirectedNegativeEdge_ThrowsNegativeCycleException()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", -1));

        Assert.Throws<NegativeCycleException>(() => Johnson<string>().Compute(graph));
    }

    [Fact]
    public void Compute_UndirectedGraph_MatchesFloydWarshall()
    {
        var random = GraphGenerator.ErdosRenyi(30, 0.1, seed: 7);
        var graph = new UndirectedGraph<int, WeightedEdge<int, int>>();
        foreach (var vertex in random.Vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var edge in random.Edges)
        {
            graph.AddEdge(new WeightedEdge<int, int>(
                edge.Source, edge.Target, (edge.Source * 13 + edge.Target) % 9 + 1));
        }

        var johnson = new JohnsonAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .Compute(graph);
        var floydWarshall = new FloydWarshallAllShortestPaths<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .Compute(graph);

        foreach (var source in graph.Vertices)
        {
            foreach (var target in graph.Vertices)
            {
                var expected = floydWarshall.Between(source, target);
                var actual = johnson.Between(source, target);
                Assert.Equal(expected.IsReachable, actual.IsReachable);
                if (expected.IsReachable)
                {
                    Assert.Equal(expected.Distance, actual.Distance);
                }
            }
        }
    }

    [Fact]
    public void Compute_DisconnectedPairs_AreUnreachable()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("z");

        var result = Johnson<string>().Compute(graph);

        Assert.False(result.Between("a", "z").IsReachable);
        Assert.False(result.Between("b", "a").IsReachable);
        Assert.True(result.Between("a", "b").IsReachable);
    }

    [Fact]
    public void Compute_ReconstructsPaths()
    {
        var graph = Directed(
            ("a", "b", -2),
            ("b", "c", 3),
            ("a", "c", 5));

        var result = Johnson<string>().Compute(graph);

        var path = result.Between("a", "c");
        Assert.Equal(1, path.Distance);
        Assert.Equal(["a", "b", "c"], path.Path);
    }

    [Fact]
    public void Compute_SelfLoopsAndParallelEdges_KeepDiagonalZeroAndCheapestArc()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "a", 5));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        var result = Johnson<string>().Compute(graph);

        Assert.Equal(0, result.Between("a", "a").Distance);
        Assert.Equal(2, result.Between("a", "b").Distance);
    }

    [Fact]
    public void Compute_ParallelOptions_IsBitIdenticalToSequential()
    {
        var random = GraphGenerator.ErdosRenyiDirected(40, 0.08, seed: 11);
        var graph = new DirectedGraph<int, WeightedEdge<int, double>>();
        foreach (var vertex in random.Vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var edge in random.Edges)
        {
            graph.AddEdge(new WeightedEdge<int, double>(
                edge.Source, edge.Target, ((edge.Source * 31 + edge.Target) % 10 + 0.25) / 3.0));
        }

        var algorithm = new JohnsonAllShortestPaths<int, WeightedEdge<int, double>, double>(e => e.Weight);
        var sequential = algorithm.Compute(graph);
        var parallel = algorithm.Compute(graph, new ParallelOptions { MaxDegreeOfParallelism = 4 });

        foreach (var source in graph.Vertices)
        {
            foreach (var target in graph.Vertices)
            {
                var expected = sequential.Between(source, target);
                var actual = parallel.Between(source, target);
                Assert.Equal(expected.IsReachable, actual.IsReachable);
                if (expected.IsReachable)
                {
                    Assert.Equal(expected.Distance, actual.Distance); // exact, not approximate
                }
            }
        }
    }

    [Fact]
    public void Compute_EmptyGraph_YieldsEmptyResult()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();

        var result = Johnson<string>().Compute(graph);

        Assert.Throws<ArgumentException>(() => result.Between("a", "b"));
    }

    [Fact]
    public void Compute_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var graph = Directed(("a", "b", 1));
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => Johnson<string>().Compute(graph, source.Token));
        Assert.Throws<OperationCanceledException>(
            () => Johnson<string>().Compute(graph, new ParallelOptions { CancellationToken = source.Token }));
    }

    [Fact]
    public void Compute_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => new JohnsonAllShortestPaths<string, WeightedEdge<string, int>, int>(null!));
        Assert.Throws<ArgumentNullException>(() => Johnson<string>().Compute(null!));
        Assert.Throws<ArgumentNullException>(
            () => Johnson<string>().Compute(Directed(("a", "b", 1)), (ParallelOptions)null!));
    }
}
