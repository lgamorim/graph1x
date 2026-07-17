using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class CentralityTests
{
    [Fact]
    public void DegreeCentrality_Star_CenterIsOne()
    {
        var graph = GraphGenerator.Star(4);

        var centrality = graph.DegreeCentrality();

        Assert.Equal(1.0, centrality[0]);
        Assert.Equal(0.25, centrality[1]);
    }

    [Fact]
    public void DegreeCentrality_SingleVertex_IsZero()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Equal(0.0, graph.DegreeCentrality()["a"]);
    }

    [Fact]
    public void ClosenessCentrality_Star_CenterIsOne()
    {
        var graph = GraphGenerator.Star(4);

        var centrality = graph.ClosenessCentrality();

        Assert.Equal(1.0, centrality[0], precision: 12);
        Assert.Equal(4.0 / 7.0, centrality[1], precision: 12);
    }

    [Fact]
    public void ClosenessCentrality_Path_MiddleBeatsEnds()
    {
        var graph = GraphGenerator.Path(3);

        var centrality = graph.ClosenessCentrality();

        Assert.Equal(1.0, centrality[1], precision: 12);
        Assert.Equal(2.0 / 3.0, centrality[0], precision: 12);
    }

    [Fact]
    public void ClosenessCentrality_DisconnectedGraph_ScalesByComponentSize()
    {
        // Wasserman-Faust scaling: a in a 2-vertex component of a 3-vertex
        // graph gets (1/2) * (1/1) = 0.5; the isolated vertex gets 0.
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddVertex("island");

        var centrality = graph.ClosenessCentrality();

        Assert.Equal(0.5, centrality["a"], precision: 12);
        Assert.Equal(0.0, centrality["island"]);
    }

    [Fact]
    public void ClosenessCentrality_Weighted_UsesTheSelector()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 2));

        var centrality = graph.ClosenessCentrality(e => e.Weight);

        Assert.Equal(2.0 / 4.0, centrality["b"], precision: 12);
        Assert.Equal(2.0 / 6.0, centrality["a"], precision: 12);
    }

    [Fact]
    public void Betweenness_Path_OnlyTheMiddleCarriesTraffic()
    {
        var graph = GraphGenerator.Path(3);

        var centrality = graph.BetweennessCentrality();

        Assert.Equal(1.0, centrality[1], precision: 12);
        Assert.Equal(0.0, centrality[0], precision: 12);
    }

    [Fact]
    public void Betweenness_Star_CenterCarriesAllPairs()
    {
        var graph = GraphGenerator.Star(4);

        var centrality = graph.BetweennessCentrality();

        Assert.Equal(6.0, centrality[0], precision: 12); // C(4,2) leaf pairs
        Assert.Equal(0.0, centrality[1], precision: 12);
    }

    [Fact]
    public void Betweenness_EvenCycle_SplitsAcrossTiedShortestPaths()
    {
        var graph = GraphGenerator.Cycle(4);

        var centrality = graph.BetweennessCentrality();

        // Each opposite pair has two tied shortest paths; each vertex sits on
        // exactly one of them, contributing 1/2.
        Assert.All(graph.Vertices, vertex => Assert.Equal(0.5, centrality[vertex], 12));
    }

    [Fact]
    public void Betweenness_DirectedChain_IsNotHalved()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal(1.0, graph.BetweennessCentrality()["b"], precision: 12);
    }

    [Fact]
    public void Betweenness_Weighted_FollowsWeightedShortestPaths()
    {
        // Unweighted, a-c direct would be shortest; weights force a-b-c.
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "c", 5));

        Assert.Equal(1.0, graph.BetweennessCentrality(e => e.Weight)["b"], precision: 12);
    }

    [Fact]
    public void Betweenness_Weighted_ZeroWeight_ThrowsNegativeWeightException()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 0.0));

        Assert.Throws<NegativeWeightException>(() => graph.BetweennessCentrality(e => e.Weight));
    }

    [Fact]
    public void Betweenness_Weighted_ZeroWeight_ThrowsWithCancellationTokenOverload()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 0.0));

        Assert.Throws<NegativeWeightException>(
            () => graph.BetweennessCentrality(e => e.Weight, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Betweenness_Weighted_ZeroWeight_ThrowsWithParallelOverload()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 0.0));

        // Parallel.ForEach wraps a body failure, per the overload's contract.
        var exception = Assert.Throws<AggregateException>(() => graph.BetweennessCentrality(
            e => e.Weight,
            new ParallelOptions { CancellationToken = TestContext.Current.CancellationToken }));

        Assert.Contains(exception.InnerExceptions, inner => inner is NegativeWeightException);
    }

    [Fact]
    public void Betweenness_Weighted_NegativeWeight_ThrowsNegativeWeightException()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", -1));

        Assert.Throws<NegativeWeightException>(() => graph.BetweennessCentrality(e => e.Weight));
    }

    [Fact]
    public void Betweenness_Weighted_ZeroWeightInSeparateComponent_IsStillRejected()
    {
        // Every vertex takes a turn as source, so the guard must fire even for
        // an edge no sweep from the first component ever relaxes.
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 1.0));
        graph.AddEdge(new WeightedEdge<string, double>("x", "y", 0.0));

        Assert.Throws<NegativeWeightException>(() => graph.BetweennessCentrality(e => e.Weight));
    }

    [Fact]
    public void Betweenness_AgreesWithBruteForce_OnSeededRandomGraph()
    {
        var random = new Random(20260705);
        var graph = new UndirectedGraph<int, Edge<int>>();
        for (var v = 0; v < 7; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 12; i++)
        {
            var a = random.Next(7);
            var b = random.Next(7);
            if (a != b)
            {
                graph.AddEdge(new Edge<int>(a, b));
            }
        }

        var brandes = graph.BetweennessCentrality();
        var bruteForce = BruteForceBetweenness(graph);

        foreach (var vertex in graph.Vertices)
        {
            Assert.Equal(bruteForce[vertex], brandes[vertex], precision: 9);
        }
    }

    [Fact]
    public void PageRank_TwoNodeCycle_IsUniform()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "a"));

        var ranks = graph.PageRank();

        Assert.Equal(0.5, ranks["a"], precision: 9);
        Assert.Equal(0.5, ranks["b"], precision: 9);
    }

    [Fact]
    public void PageRank_SumsToOne_WithDanglingNodes()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("c", "b")); // b is a dangling sink

        var ranks = graph.PageRank();

        Assert.Equal(1.0, ranks.Values.Sum(), precision: 9);
        Assert.True(ranks["b"] > ranks["a"]);
    }

    [Fact]
    public void PageRank_HubReceivingAllLinks_Dominates()
    {
        var graph = new DirectedGraph<int, Edge<int>>();
        for (var leaf = 1; leaf <= 4; leaf++)
        {
            graph.AddEdge(new Edge<int>(leaf, 0));
        }

        var ranks = graph.PageRank();

        Assert.True(ranks[0] > 0.5);
        Assert.Equal(1.0, ranks.Values.Sum(), precision: 9);
    }

    [Fact]
    public void PageRank_InvalidArguments_Throw()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Throws<ArgumentOutOfRangeException>(() => graph.PageRank(damping: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.PageRank(damping: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.PageRank(maxIterations: 0));
    }

    [Fact]
    public void EmptyGraph_YieldsEmptyCentralities()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.DegreeCentrality());
        Assert.Empty(graph.ClosenessCentrality());
        Assert.Empty(graph.BetweennessCentrality());
    }

    /// <summary>Reference implementation: enumerate every simple path per pair, keep the shortest, count pass-throughs.</summary>
    private static Dictionary<int, double> BruteForceBetweenness(UndirectedGraph<int, Edge<int>> graph)
    {
        var centrality = graph.Vertices.ToDictionary(v => v, _ => 0.0);
        var vertices = graph.Vertices.ToList();

        foreach (var source in vertices)
        {
            foreach (var target in vertices.Where(t => t > source))
            {
                var paths = new List<List<int>>();
                EnumeratePaths(graph, source, target, [source], paths);
                if (paths.Count == 0)
                {
                    continue;
                }

                var shortest = paths.Min(p => p.Count);
                var shortestPaths = paths.Where(p => p.Count == shortest).ToList();
                foreach (var path in shortestPaths)
                {
                    foreach (var inner in path.Skip(1).Take(path.Count - 2))
                    {
                        centrality[inner] += 1.0 / shortestPaths.Count;
                    }
                }
            }
        }

        return centrality;
    }

    private static void EnumeratePaths(
        UndirectedGraph<int, Edge<int>> graph,
        int current,
        int target,
        List<int> path,
        List<List<int>> found)
    {
        if (current == target)
        {
            found.Add([.. path]);
            return;
        }

        foreach (var edge in graph.AdjacentEdges(current))
        {
            var next = edge.Source == current ? edge.Target : edge.Source;
            if (!path.Contains(next))
            {
                path.Add(next);
                EnumeratePaths(graph, next, target, path, found);
                path.RemoveAt(path.Count - 1);
            }
        }
    }
}
