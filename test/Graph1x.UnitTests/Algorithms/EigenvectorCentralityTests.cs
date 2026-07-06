using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class EigenvectorCentralityTests
{
    private const int Precision = 6;

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
    public void EigenvectorCentrality_OnATriangle_IsUniform()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"));

        var centrality = graph.EigenvectorCentrality();

        var expected = 1.0 / Math.Sqrt(3.0);
        Assert.Equal(expected, centrality["a"], Precision);
        Assert.Equal(expected, centrality["b"], Precision);
        Assert.Equal(expected, centrality["c"], Precision);
    }

    [Fact]
    public void EigenvectorCentrality_OnAPath_MatchesTheKnownEigenvector()
    {
        var graph = Undirected(("a", "b"), ("b", "c"));

        var centrality = graph.EigenvectorCentrality();

        // P3's principal eigenvector is (1, √2, 1) / 2.
        Assert.Equal(0.5, centrality["a"], Precision);
        Assert.Equal(Math.Sqrt(2.0) / 2.0, centrality["b"], Precision);
        Assert.Equal(0.5, centrality["c"], Precision);
    }

    [Fact]
    public void EigenvectorCentrality_OnAStar_RanksTheHubHighest()
    {
        var graph = Undirected(("hub", "a"), ("hub", "b"), ("hub", "c"));

        var centrality = graph.EigenvectorCentrality();

        Assert.True(centrality["hub"] > centrality["a"]);
        Assert.Equal(centrality["a"], centrality["b"], Precision);
    }

    [Fact]
    public void EigenvectorCentrality_OnADirectedCycle_IsUniform()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "a"));

        var centrality = graph.EigenvectorCentrality();

        var expected = 1.0 / Math.Sqrt(3.0);
        Assert.Equal(expected, centrality["a"], Precision);
        Assert.Equal(expected, centrality["b"], Precision);
        Assert.Equal(expected, centrality["c"], Precision);
    }

    [Fact]
    public void EigenvectorCentrality_OnADag_DriftsTowardTheSinks()
    {
        // Acyclic structure is the documented degenerate case (Katz is the
        // answer there): scores concentrate on downstream vertices.
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var centrality = graph.EigenvectorCentrality();

        Assert.True(centrality["c"] > centrality["b"]);
        Assert.True(centrality["b"] > centrality["a"]);
        Assert.True(centrality["a"] >= 0.0);
    }

    [Fact]
    public void EigenvectorCentrality_OnAnEdgelessGraph_IsUniform()
    {
        // Every vector is an eigenvector of the zero matrix; the shifted
        // iteration settles on the uniform one.
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        var centrality = graph.EigenvectorCentrality();

        Assert.Equal(1.0 / Math.Sqrt(2.0), centrality["a"], Precision);
        Assert.Equal(1.0 / Math.Sqrt(2.0), centrality["b"], Precision);
    }

    [Fact]
    public void EigenvectorCentrality_OnAMultigraph_WeighsParallelEdges()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("hub", "a"));
        graph.AddEdge(new Edge<string>("hub", "a")); // doubled pull toward a
        graph.AddEdge(new Edge<string>("hub", "b"));

        var centrality = graph.EigenvectorCentrality();

        Assert.True(centrality["a"] > centrality["b"]);
    }

    [Fact]
    public void EigenvectorCentrality_OnAnEmptyGraph_IsEmpty()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.EigenvectorCentrality());
    }

    [Fact]
    public void EigenvectorCentrality_ValidatesArguments()
    {
        var graph = Undirected(("a", "b"));

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.EigenvectorCentrality());
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.EigenvectorCentrality(maxIterations: 0));
    }

    [Fact]
    public void EigenvectorCentrality_ObservesCancellation()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"));
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => graph.EigenvectorCentrality(cancellationToken: source.Token));
    }
}
