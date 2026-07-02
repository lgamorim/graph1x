using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class StructureTests
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

    private static DirectedGraph<string, Edge<string>> Directed(params (string, string)[] edges)
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    [Fact]
    public void Density_EmptyOrSingleVertex_IsZero()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Equal(0.0, graph.Density());

        graph.AddVertex("a");

        Assert.Equal(0.0, graph.Density());
    }

    [Fact]
    public void Density_CompleteUndirectedGraph_IsOne()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("a", "c"));

        Assert.Equal(1.0, graph.Density());
    }

    [Fact]
    public void Density_CompleteDirectedGraph_IsOne()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        Assert.Equal(1.0, graph.Density());
    }

    [Fact]
    public void Density_SparseDirectedGraph_IsProportional()
    {
        var graph = Directed(("a", "b"));
        graph.AddVertex("c");

        Assert.Equal(1.0 / 6.0, graph.Density(), precision: 10);
    }

    [Fact]
    public void DegreeSequence_IsDescending()
    {
        var graph = Undirected(("a", "b"), ("a", "c"), ("a", "d"), ("b", "c"));

        Assert.Equal([3, 2, 2, 1], graph.DegreeSequence());
    }

    [Fact]
    public void DegreeSequence_EmptyGraph_IsEmpty()
    {
        Assert.Empty(new UndirectedGraph<string, Edge<string>>().DegreeSequence());
    }

    [Fact]
    public void IsBipartite_EvenCycle_IsTrue()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"));

        Assert.True(graph.IsBipartite());
    }

    [Fact]
    public void IsBipartite_OddCycle_IsFalse()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        Assert.False(graph.IsBipartite());
        Assert.Null(graph.FindBipartition());
    }

    [Fact]
    public void IsBipartite_EmptyAndSingleVertex_AreTrue()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.True(graph.IsBipartite());

        graph.AddVertex("a");

        Assert.True(graph.IsBipartite());
    }

    [Fact]
    public void IsBipartite_SelfLoop_IsFalse()
    {
        Assert.False(Undirected(("a", "a")).IsBipartite());
    }

    [Fact]
    public void FindBipartition_ReturnsValidTwoColoring()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"), ("x", "y"));

        var partition = graph.FindBipartition();

        Assert.NotNull(partition);
        var (left, right) = partition.Value;

        Assert.Equal(graph.VertexCount, left.Count + right.Count);
        foreach (var edge in graph.Edges)
        {
            Assert.NotEqual(left.Contains(edge.Source), left.Contains(edge.Target));
        }
    }

    [Fact]
    public void IsBipartite_DirectedGraph_IgnoresDirection()
    {
        // a->b, c->b, a->c forms an odd cycle when direction is ignored.
        var graph = Directed(("a", "b"), ("c", "b"), ("a", "c"));

        Assert.False(graph.IsBipartite());
    }

    [Fact]
    public void Transpose_ReversesAllEdges()
    {
        var graph = Directed(("a", "b"), ("b", "c"));
        graph.AddVertex("lonely");

        var transposed = graph.Transpose();

        Assert.True(transposed.ContainsEdge("b", "a"));
        Assert.True(transposed.ContainsEdge("c", "b"));
        Assert.False(transposed.ContainsEdge("a", "b"));
        Assert.True(transposed.ContainsVertex("lonely"));
        Assert.Equal(graph.EdgeCount, transposed.EdgeCount);
    }

    [Fact]
    public void Transpose_WeightedEdges_KeepWeights()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        var transposed = graph.Transpose();

        Assert.Contains(new WeightedEdge<string, int>("b", "a", 7), transposed.Edges);
    }

    [Fact]
    public void Transpose_Multigraph_KeepsParallelEdges()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var transposed = graph.Transpose(edge => new Edge<string>(edge.Target, edge.Source));

        Assert.Equal(2, transposed.EdgeCount);
        Assert.True(transposed.AllowsParallelEdges);
    }

    [Fact]
    public void Transpose_TwiceRestoresOriginalEdges()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("a", "c"));

        var roundTripped = graph.Transpose().Transpose();

        Assert.Equal(graph.EdgeCount, roundTripped.EdgeCount);
        foreach (var edge in graph.Edges)
        {
            Assert.True(roundTripped.ContainsEdge(edge.Source, edge.Target));
        }
    }
}
