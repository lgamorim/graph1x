using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class KatzCentralityTests
{
    private const int Precision = 6;

    [Fact]
    public void KatzCentrality_OnADag_IsNonDegenerate()
    {
        // The case eigenvector centrality can't handle: downstream vertices
        // accumulate strictly more score.
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var centrality = graph.KatzCentrality();

        Assert.True(centrality["a"] > 0.0);
        Assert.True(centrality["b"] > centrality["a"]);
        Assert.True(centrality["c"] > centrality["b"]);
    }

    [Fact]
    public void KatzCentrality_OnADirectedChain_MatchesTheClosedForm()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var centrality = graph.KatzCentrality(alpha: 0.1, beta: 1.0);

        // Unnormalized scores are (1, 1.1, 1.11); the result is L2-normalized.
        var norm = Math.Sqrt((1.0 * 1.0) + (1.1 * 1.1) + (1.11 * 1.11));
        Assert.Equal(1.0 / norm, centrality["a"], Precision);
        Assert.Equal(1.1 / norm, centrality["b"], Precision);
        Assert.Equal(1.11 / norm, centrality["c"], Precision);
    }

    [Fact]
    public void KatzCentrality_OnASymmetricGraph_IsUniform()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("a", "c"));

        var centrality = graph.KatzCentrality();

        Assert.Equal(centrality["a"], centrality["b"], Precision);
        Assert.Equal(centrality["b"], centrality["c"], Precision);
    }

    [Fact]
    public void KatzCentrality_SingleVertex_ScoresOne()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        var centrality = graph.KatzCentrality();

        Assert.Equal(1.0, centrality["a"], Precision);
    }

    [Fact]
    public void KatzCentrality_OnAnEmptyGraph_IsEmpty()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.KatzCentrality());
    }

    [Fact]
    public void KatzCentrality_ValidatesArguments()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.KatzCentrality());
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.KatzCentrality(alpha: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.KatzCentrality(alpha: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.KatzCentrality(maxIterations: 0));
    }

    [Fact]
    public void KatzCentrality_ObservesCancellation()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => graph.KatzCentrality(cancellationToken: source.Token));
    }
}
