using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class BipartiteMatchingTests
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

    /// <summary>Asserts the matching is valid: edges exist in the graph and no vertex is matched twice.</summary>
    private static void AssertValidMatching<TEdge>(
        IReadOnlyGraph<string, TEdge> graph,
        IReadOnlyList<TEdge> matching)
        where TEdge : Graph1x.Edges.IEdge<string>
    {
        var used = new HashSet<string>();
        foreach (var edge in matching)
        {
            Assert.True(graph.ContainsEdge(edge.Source, edge.Target));
            Assert.True(used.Add(edge.Source), $"{edge.Source} is matched twice");
            Assert.True(used.Add(edge.Target), $"{edge.Target} is matched twice");
        }
    }

    [Fact]
    public void CompleteTwoByTwo_HasPerfectMatching()
    {
        var graph = Undirected(("l1", "r1"), ("l1", "r2"), ("l2", "r1"), ("l2", "r2"));

        var matching = graph.MaximumBipartiteMatching();

        Assert.Equal(2, matching.Count);
        AssertValidMatching(graph, matching);
    }

    [Fact]
    public void Path_OfTwoEdges_MatchesOne()
    {
        var graph = Undirected(("a", "b"), ("b", "c"));

        Assert.Single(graph.MaximumBipartiteMatching());
    }

    [Fact]
    public void Path_OfThreeEdges_MatchesTwo()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"));

        var matching = graph.MaximumBipartiteMatching();

        Assert.Equal(2, matching.Count);
        AssertValidMatching(graph, matching);
    }

    [Fact]
    public void AugmentingPath_IsFound()
    {
        // Greedy could match l1-r1 and strand l2; Hopcroft-Karp must augment to 2.
        var graph = Undirected(("l1", "r1"), ("l1", "r2"), ("l2", "r1"));

        Assert.Equal(2, graph.MaximumBipartiteMatching().Count);
    }

    [Fact]
    public void ThreeLeftTwoRight_MatchesAtMostTwo()
    {
        var graph = Undirected(
            ("l1", "r1"), ("l1", "r2"), ("l2", "r1"), ("l2", "r2"), ("l3", "r1"), ("l3", "r2"));

        Assert.Equal(2, graph.MaximumBipartiteMatching().Count);
    }

    [Fact]
    public void EmptyGraph_HasEmptyMatching()
    {
        Assert.Empty(new UndirectedGraph<string, Edge<string>>().MaximumBipartiteMatching());
    }

    [Fact]
    public void IsolatedVertices_HaveEmptyMatching()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        Assert.Empty(graph.MaximumBipartiteMatching());
    }

    [Fact]
    public void DisconnectedComponents_MatchingsAdd()
    {
        var graph = Undirected(("a", "b"), ("x", "y"), ("y", "z"));
        graph.AddVertex("lonely");

        var matching = graph.MaximumBipartiteMatching();

        Assert.Equal(2, matching.Count);
        AssertValidMatching(graph, matching);
    }

    [Fact]
    public void ParallelEdges_CountOnce()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Single(graph.MaximumBipartiteMatching());
    }

    [Fact]
    public void NonBipartiteGraph_Throws()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        Assert.Throws<ArgumentException>(() => graph.MaximumBipartiteMatching());
    }

    [Fact]
    public void DirectedGraph_Throws()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var algorithm = new HopcroftKarpMatching<string, Edge<string>>();

        Assert.Throws<ArgumentException>(() => algorithm.FindMaximumMatching(graph));
    }

    [Fact]
    public void MatchingSize_AgreesWithUnitCapacityMaxFlow_OnSeededRandomGraph()
    {
        var random = new Random(20260702);
        var graph = new UndirectedGraph<string, Edge<string>>();
        for (var i = 0; i < 8; i++)
        {
            graph.AddVertex($"l{i}");
            graph.AddVertex($"r{i}");
        }

        for (var i = 0; i < 20; i++)
        {
            graph.AddEdge(new Edge<string>($"l{random.Next(8)}", $"r{random.Next(8)}"));
        }

        // König/flow duality: matching size equals max flow in the unit network
        // super-source -> left -> right -> super-sink.
        var network = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var edge in graph.Edges)
        {
            var (left, right) = edge.Source.StartsWith('l') ? (edge.Source, edge.Target) : (edge.Target, edge.Source);
            network.AddEdge(new WeightedEdge<string, int>(left, right, 1));
        }

        for (var i = 0; i < 8; i++)
        {
            if (network.ContainsVertex($"l{i}"))
            {
                network.AddEdge(new WeightedEdge<string, int>("SOURCE", $"l{i}", 1));
            }

            if (network.ContainsVertex($"r{i}"))
            {
                network.AddEdge(new WeightedEdge<string, int>($"r{i}", "SINK", 1));
            }
        }

        var matching = graph.MaximumBipartiteMatching();
        var flow = network.MaximumFlow("SOURCE", "SINK").FlowValue;

        Assert.Equal(flow, matching.Count);
        AssertValidMatching(graph, matching);
    }

    [Fact]
    public void Strategy_And_Facade_Agree()
    {
        var graph = Undirected(("a", "b"), ("c", "d"));

        var viaStrategy = new HopcroftKarpMatching<string, Edge<string>>().FindMaximumMatching(graph);
        var viaFacade = graph.MaximumBipartiteMatching();

        Assert.Equal(viaStrategy.Count, viaFacade.Count);
    }
}
