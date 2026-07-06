using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class GraphOperationsTests
{
    private static DirectedGraph<string, Edge<string>> Directed(params (string, string)[] edges)
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    private static UndirectedGraph<string, Edge<string>> Undirected(params (string, string)[] edges)
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    // ---- Subgraph ----

    [Fact]
    public void Subgraph_KeepsOnlyEdgesWithBothEndpointsSelected()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("a", "c"));

        var subgraph = graph.Subgraph(["a", "b"]);

        Assert.Equal(2, subgraph.VertexCount);
        Assert.Equal(1, subgraph.EdgeCount);
        Assert.True(subgraph.ContainsEdge("a", "b"));
        Assert.False(subgraph.ContainsVertex("c"));
    }

    [Fact]
    public void Subgraph_IgnoresUnknownVertices()
    {
        var graph = Directed(("a", "b"));

        var subgraph = graph.Subgraph(["a", "x"]);

        Assert.Equal(1, subgraph.VertexCount);
        Assert.True(subgraph.ContainsVertex("a"));
        Assert.False(subgraph.ContainsVertex("x"));
    }

    [Fact]
    public void Subgraph_KeepsSelectedIsolatedVertices()
    {
        var graph = Directed(("a", "b"));
        graph.AddVertex("d");

        var subgraph = graph.Subgraph(["a", "d"]);

        Assert.Equal(2, subgraph.VertexCount);
        Assert.Equal(0, subgraph.EdgeCount);
        Assert.True(subgraph.ContainsVertex("d"));
    }

    [Fact]
    public void Subgraph_EmptySelection_YieldsEmptyGraphOfSameFamily()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var subgraph = graph.Subgraph([]);

        Assert.Equal(0, subgraph.VertexCount);
        Assert.Equal(0, subgraph.EdgeCount);
        Assert.True(subgraph.IsDirected);
        Assert.True(subgraph.AllowsParallelEdges);
    }

    [Fact]
    public void Subgraph_KeepsParallelEdgesAndSelfLoops()
    {
        var graph = new UndirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("a", "a", 3));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 4));

        var subgraph = graph.Subgraph(["a", "b"]);

        Assert.Equal(3, subgraph.EdgeCount);
        Assert.Equal(2, subgraph.Edges.Count(e => e.Source == "a" && e.Target == "b"));
        Assert.True(subgraph.ContainsEdge("a", "a"));
    }

    [Fact]
    public void Subgraph_PreservesTheVertexComparer()
    {
        var graph = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("Lisbon", "Porto"));

        var subgraph = graph.Subgraph(["LISBON", "porto"]);

        Assert.True(subgraph.ContainsEdge("PORTO", "lisbon"));
    }

    [Fact]
    public void Subgraph_DuplicateSelectionEntries_AreHarmless()
    {
        var graph = Directed(("a", "b"));

        var subgraph = graph.Subgraph(["a", "a", "b", "b"]);

        Assert.Equal(2, subgraph.VertexCount);
        Assert.Equal(1, subgraph.EdgeCount);
    }

    [Fact]
    public void Subgraph_OnDirectedGraph_KeepsTheDirectedStaticType()
    {
        var graph = Directed(("a", "b"), ("b", "c"));

        IDirectedGraph<string, Edge<string>> subgraph = graph.Subgraph(["a", "b"]);

        Assert.Equal(1, subgraph.OutDegree("a"));
        Assert.Equal(0, subgraph.InDegree("a"));
    }

    [Fact]
    public void Subgraph_OnMatrixGraphs_KeepsTheInducedEdges()
    {
        var directed = new DirectedAdjacencyMatrixGraph<string, Edge<string>>();
        directed.AddEdge(new Edge<string>("a", "b"));
        directed.AddEdge(new Edge<string>("b", "c"));

        var subgraph = directed.Subgraph(["a", "b"]);

        Assert.True(subgraph.IsDirected);
        Assert.True(subgraph.ContainsEdge("a", "b"));
        Assert.False(subgraph.ContainsEdge("b", "a"));
        Assert.Equal(1, subgraph.EdgeCount);
    }

    [Fact]
    public void Subgraph_DoesNotMutateTheSource()
    {
        var graph = Directed(("a", "b"), ("b", "c"));

        graph.Subgraph(["a"]);

        Assert.Equal(3, graph.VertexCount);
        Assert.Equal(2, graph.EdgeCount);
    }

    [Fact]
    public void Subgraph_NullArguments_Throw()
    {
        var graph = Directed(("a", "b"));

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.Subgraph(["a"]));
        Assert.Throws<ArgumentNullException>(() => graph.Subgraph(null!));
    }

    // ---- Union ----

    [Fact]
    public void Union_MergesVerticesAndEdges()
    {
        var first = Undirected(("a", "b"));
        var second = Undirected(("c", "d"));

        var union = first.Union(second);

        Assert.Equal(4, union.VertexCount);
        Assert.Equal(2, union.EdgeCount);
        Assert.True(union.ContainsEdge("a", "b"));
        Assert.True(union.ContainsEdge("c", "d"));
    }

    [Fact]
    public void Union_OnSimpleGraphs_DedupesSharedEdges()
    {
        var first = Undirected(("a", "b"), ("b", "c"));
        var second = Undirected(("b", "c"), ("c", "d"));

        var union = first.Union(second);

        Assert.Equal(3, union.EdgeCount);
    }

    [Fact]
    public void Union_OnMultigraphs_KeepsEdgesFromBothOperands()
    {
        var first = new UndirectedMultigraph<string, Edge<string>>();
        first.AddEdge(new Edge<string>("a", "b"));
        var second = new UndirectedMultigraph<string, Edge<string>>();
        second.AddEdge(new Edge<string>("a", "b"));

        var union = first.Union(second);

        Assert.Equal(2, union.EdgeCount);
        Assert.Equal(2, union.Edges.Count(e => e.Source == "a" && e.Target == "b"));
    }

    [Fact]
    public void Union_MixedDirectedness_Throws()
    {
        var directed = Directed(("a", "b"));
        var undirected = Undirected(("a", "b"));

        Assert.Throws<ArgumentException>(
            () => ((IReadOnlyGraph<string, Edge<string>>)directed).Union(undirected));
    }

    [Fact]
    public void Union_ResultFamilyAndComparerComeFromTheFirstOperand()
    {
        var first = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        first.AddEdge(new Edge<string>("Lisbon", "Porto"));
        var second = Undirected(("LISBON", "Faro"));

        var union = first.Union(second);

        Assert.False(union.AllowsParallelEdges);
        Assert.Equal(3, union.VertexCount); // LISBON folds into Lisbon
        Assert.True(union.ContainsEdge("lisbon", "faro"));
    }

    [Fact]
    public void Union_OnDirectedGraphs_KeepsTheDirectedStaticType()
    {
        var first = Directed(("a", "b"));
        var second = Directed(("b", "c"));

        IDirectedGraph<string, Edge<string>> union = first.Union(second);

        Assert.Equal(1, union.OutDegree("b"));
        Assert.Equal(1, union.InDegree("b"));
        Assert.False(union.ContainsEdge("b", "a"));
    }

    [Fact]
    public void Union_DoesNotMutateTheOperands()
    {
        var first = Undirected(("a", "b"));
        var second = Undirected(("c", "d"));

        first.Union(second);

        Assert.Equal(1, first.EdgeCount);
        Assert.Equal(1, second.EdgeCount);
    }

    [Fact]
    public void Union_NullArguments_Throw()
    {
        var graph = Undirected(("a", "b"));

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.Union(graph));
        Assert.Throws<ArgumentNullException>(
            () => graph.Union(default(IReadOnlyGraph<string, Edge<string>>)!));
    }

    // ---- Complement ----

    [Fact]
    public void Complement_OfACompleteGraph_IsEdgeless()
    {
        var graph = Undirected(("a", "b"), ("a", "c"), ("b", "c"));

        var complement = graph.Complement();

        Assert.Equal(3, complement.VertexCount);
        Assert.Equal(0, complement.EdgeCount);
    }

    [Fact]
    public void Complement_OfAnEdgelessGraph_IsComplete()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");
        graph.AddVertex("c");
        graph.AddVertex("d");

        var complement = graph.Complement();

        Assert.Equal(6, complement.EdgeCount); // C(4,2)
    }

    [Fact]
    public void Complement_OnDirectedGraphs_CoversAllOrderedPairs()
    {
        var graph = Directed(("a", "b"));
        graph.AddVertex("c");

        IDirectedGraph<string, Edge<string>> complement = graph.Complement();

        // 3·2 ordered pairs minus the existing a->b.
        Assert.Equal(5, complement.EdgeCount);
        Assert.False(complement.ContainsEdge("a", "b"));
        Assert.True(complement.ContainsEdge("b", "a"));
    }

    [Fact]
    public void Complement_NeverEmitsSelfLoops()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        var complement = graph.Complement();

        Assert.Equal(0, complement.EdgeCount);
        Assert.False(complement.ContainsEdge("a", "a"));
    }

    [Fact]
    public void Complement_AppliedTwice_RestoresTheEdgeSet()
    {
        var graph = Undirected(("a", "b"), ("c", "d"));

        var restored = graph.Complement().Complement();

        Assert.Equal(graph.VertexCount, restored.VertexCount);
        Assert.Equal(graph.EdgeCount, restored.EdgeCount);
        Assert.True(restored.ContainsEdge("a", "b"));
        Assert.True(restored.ContainsEdge("c", "d"));
        Assert.False(restored.ContainsEdge("a", "c"));
    }

    [Fact]
    public void Complement_UsesTheEdgeFactory()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        var complement = graph.Complement((s, t) => new WeightedEdge<string, int>(s, t, 7));

        var edge = Assert.Single(complement.Edges);
        Assert.Equal(7, edge.Weight);
    }

    [Fact]
    public void Complement_PreservesTheVertexComparer()
    {
        var graph = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddVertex("Lisbon");
        graph.AddVertex("Porto");

        var complement = graph.Complement();

        Assert.True(complement.ContainsEdge("LISBON", "porto"));
    }

    [Fact]
    public void Complement_OnAMultigraph_Throws()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.Complement());
    }

    [Fact]
    public void Complement_NullArguments_Throw()
    {
        var graph = Undirected(("a", "b"));

        Assert.Throws<ArgumentNullException>(
            () => default(IReadOnlyGraph<string, Edge<string>>)!.Complement(
                (s, t) => new Edge<string>(s, t)));
        Assert.Throws<ArgumentNullException>(() => graph.Complement(null!));
    }
}
