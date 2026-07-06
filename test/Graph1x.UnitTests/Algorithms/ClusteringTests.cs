using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class ClusteringTests
{
    private const int Precision = 12;

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
    public void LocalClusteringCoefficient_OnATriangle_IsOneEverywhere()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"));

        Assert.Equal(1.0, graph.LocalClusteringCoefficient("a"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("b"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("c"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_OnAStar_IsZeroEverywhere()
    {
        var graph = Undirected(("hub", "a"), ("hub", "b"), ("hub", "c"));

        Assert.Equal(0.0, graph.LocalClusteringCoefficient("hub"), Precision);
        Assert.Equal(0.0, graph.LocalClusteringCoefficient("a"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_DegreeBelowTwo_IsZero()
    {
        var graph = Undirected(("a", "b"));
        graph.AddVertex("isolated");

        Assert.Equal(0.0, graph.LocalClusteringCoefficient("a"), Precision);
        Assert.Equal(0.0, graph.LocalClusteringCoefficient("isolated"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_SquareWithOneDiagonal()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"), ("a", "c"));

        // a and c see three neighbors with two links among them; b and d see
        // two neighbors that are themselves linked.
        Assert.Equal(2.0 / 3.0, graph.LocalClusteringCoefficient("a"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("b"), Precision);
        Assert.Equal(2.0 / 3.0, graph.LocalClusteringCoefficient("c"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("d"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_IgnoresSelfLoops()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        // The self-loop does not make 'a' its own neighbor: degree stays 1.
        Assert.Equal(0.0, graph.LocalClusteringCoefficient("a"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_OnAMultigraph_CountsDistinctNeighbors()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b")); // parallel: still one neighbor
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("a", "c"));

        Assert.Equal(1.0, graph.LocalClusteringCoefficient("a"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_IgnoresEdgeDirection()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "a")); // reciprocal pair is one neighbor link
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "a"));

        Assert.Equal(1.0, graph.LocalClusteringCoefficient("a"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("b"), Precision);
        Assert.Equal(1.0, graph.LocalClusteringCoefficient("c"), Precision);
    }

    [Fact]
    public void LocalClusteringCoefficient_UnknownVertex_Throws()
    {
        var graph = Undirected(("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.LocalClusteringCoefficient("x"));
    }

    [Fact]
    public void ClusteringCoefficients_CoverEveryVertex()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"), ("c", "d"));

        var coefficients = graph.ClusteringCoefficients();

        Assert.Equal(4, coefficients.Count);
        Assert.Equal(1.0, coefficients["a"], Precision);
        Assert.Equal(1.0 / 3.0, coefficients["c"], Precision);
        Assert.Equal(0.0, coefficients["d"], Precision);
    }

    [Fact]
    public void ClusteringCoefficients_OnAnEmptyGraph_AreEmpty()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.ClusteringCoefficients());
    }

    [Fact]
    public void AverageClusteringCoefficient_IsTheMeanOfTheLocals()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"), ("a", "c"));

        // (2/3 + 1 + 2/3 + 1) / 4
        Assert.Equal(5.0 / 6.0, graph.AverageClusteringCoefficient(), Precision);
    }

    [Fact]
    public void AverageClusteringCoefficient_OnAnEmptyGraph_IsZero()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Equal(0.0, graph.AverageClusteringCoefficient(), Precision);
    }

    [Fact]
    public void GlobalClusteringCoefficient_OnATriangle_IsOne()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"));

        Assert.Equal(1.0, graph.GlobalClusteringCoefficient(), Precision);
    }

    [Fact]
    public void GlobalClusteringCoefficient_OnAStar_IsZero()
    {
        var graph = Undirected(("hub", "a"), ("hub", "b"), ("hub", "c"));

        Assert.Equal(0.0, graph.GlobalClusteringCoefficient(), Precision);
    }

    [Fact]
    public void GlobalClusteringCoefficient_SquareWithOneDiagonal()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"), ("a", "c"));

        // Two triangles counted three times each over eight connected triples.
        Assert.Equal(0.75, graph.GlobalClusteringCoefficient(), Precision);
    }

    [Fact]
    public void GlobalClusteringCoefficient_WithoutConnectedTriples_IsZero()
    {
        var graph = Undirected(("a", "b"));

        Assert.Equal(0.0, graph.GlobalClusteringCoefficient(), Precision);
    }

    [Fact]
    public void ClusteringExtensions_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.ClusteringCoefficients());
        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.GlobalClusteringCoefficient());
        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.AverageClusteringCoefficient());
    }
}
