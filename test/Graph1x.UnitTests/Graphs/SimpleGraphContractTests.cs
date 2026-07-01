using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

/// <summary>
/// Contract tests every simple (non-parallel-edge) mutable graph implementation
/// must satisfy. Derived classes supply the concrete graph via the factory
/// methods; xUnit runs every inherited fact against each implementation.
/// </summary>
public abstract class SimpleGraphContractTests
{
    protected abstract IMutableGraph<string, Edge<string>> CreateGraph();

    protected abstract IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer);

    [Fact]
    public void NewGraph_IsEmpty()
    {
        var graph = CreateGraph();

        Assert.Equal(0, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
        Assert.Empty(graph.Vertices);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void NewGraph_DisallowsParallelEdges()
    {
        Assert.False(CreateGraph().AllowsParallelEdges);
    }

    [Fact]
    public void AddVertex_NewVertex_ReturnsTrueAndContains()
    {
        var graph = CreateGraph();

        Assert.True(graph.AddVertex("a"));
        Assert.True(graph.ContainsVertex("a"));
        Assert.Equal(1, graph.VertexCount);
    }

    [Fact]
    public void AddVertex_Duplicate_ReturnsFalse()
    {
        var graph = CreateGraph();
        graph.AddVertex("a");

        Assert.False(graph.AddVertex("a"));
        Assert.Equal(1, graph.VertexCount);
    }

    [Fact]
    public void AddVertex_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateGraph().AddVertex(null!));
    }

    [Fact]
    public void AddEdge_AutoAddsMissingEndpoints()
    {
        var graph = CreateGraph();

        Assert.True(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.True(graph.ContainsVertex("a"));
        Assert.True(graph.ContainsVertex("b"));
        Assert.Equal(2, graph.VertexCount);
        Assert.Equal(1, graph.EdgeCount);
        Assert.True(graph.ContainsEdge("a", "b"));
    }

    [Fact]
    public void AddEdge_DuplicateEndpoints_ReturnsFalse()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.False(graph.AddEdge(new Edge<string>("a", "b")));
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void AddEdge_SelfLoop_IsAllowed()
    {
        var graph = CreateGraph();

        Assert.True(graph.AddEdge(new Edge<string>("a", "a")));
        Assert.True(graph.ContainsEdge("a", "a"));
        Assert.Equal(1, graph.EdgeCount);
        Assert.Equal(2, graph.Degree("a"));
    }

    [Fact]
    public void AdjacentEdges_SelfLoop_YieldedOnce()
    {
        var graph = CreateGraph();
        var loop = new Edge<string>("a", "a");
        graph.AddEdge(loop);

        Assert.Equal([loop], graph.AdjacentEdges("a"));
    }

    [Fact]
    public void RemoveVertex_CascadesIncidentEdges()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("c", "a"));

        Assert.True(graph.RemoveVertex("b"));
        Assert.False(graph.ContainsVertex("b"));
        Assert.Equal(1, graph.EdgeCount);
        Assert.False(graph.ContainsEdge("a", "b"));
        Assert.False(graph.ContainsEdge("b", "c"));
        Assert.True(graph.ContainsEdge("c", "a"));
    }

    [Fact]
    public void RemoveVertex_WithSelfLoop_RemovesLoop()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.True(graph.RemoveVertex("a"));
        Assert.Equal(0, graph.EdgeCount);
        Assert.Equal(0, graph.VertexCount);
    }

    [Fact]
    public void RemoveVertex_Missing_ReturnsFalse()
    {
        Assert.False(CreateGraph().RemoveVertex("ghost"));
    }

    [Fact]
    public void RemoveVertex_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateGraph().RemoveVertex(null!));
    }

    [Fact]
    public void RemoveEdge_RemovesEdge_KeepsVertices()
    {
        var graph = CreateGraph();
        var edge = new Edge<string>("a", "b");
        graph.AddEdge(edge);

        Assert.True(graph.RemoveEdge(edge));
        Assert.Equal(0, graph.EdgeCount);
        Assert.False(graph.ContainsEdge("a", "b"));
        Assert.True(graph.ContainsVertex("a"));
        Assert.True(graph.ContainsVertex("b"));
    }

    [Fact]
    public void RemoveEdge_Missing_ReturnsFalse()
    {
        var graph = CreateGraph();
        graph.AddVertex("a");
        graph.AddVertex("b");

        Assert.False(graph.RemoveEdge(new Edge<string>("a", "b")));
    }

    [Fact]
    public void Clear_EmptiesGraph()
    {
        var graph = CreateGraph();
        graph.AddEdge(new Edge<string>("a", "b"));

        graph.Clear();

        Assert.Equal(0, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
        Assert.Empty(graph.Vertices);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void ContainsVertex_Missing_ReturnsFalse()
    {
        Assert.False(CreateGraph().ContainsVertex("ghost"));
    }

    [Fact]
    public void ContainsEdge_MissingVertices_ReturnsFalse()
    {
        Assert.False(CreateGraph().ContainsEdge("a", "b"));
    }

    [Fact]
    public void Degree_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateGraph().Degree("ghost"));
    }

    [Fact]
    public void Degree_IsolatedVertex_IsZero()
    {
        var graph = CreateGraph();
        graph.AddVertex("a");

        Assert.Equal(0, graph.Degree("a"));
    }

    [Fact]
    public void AdjacentEdges_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateGraph().AdjacentEdges("ghost").ToList());
    }

    [Fact]
    public void Vertices_MutatedDuringEnumeration_Throws()
    {
        var graph = CreateGraph();
        graph.AddVertex("a");
        graph.AddVertex("b");

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in graph.Vertices)
            {
                graph.AddVertex("c");
            }
        });
    }

    [Fact]
    public void CustomComparer_IdentifiesVerticesThroughIt()
    {
        var graph = CreateGraph(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("Lisbon", "Porto"));

        Assert.True(graph.ContainsVertex("LISBON"));
        Assert.False(graph.AddVertex("lisbon"));
        Assert.True(graph.ContainsEdge("lisbon", "PORTO"));
        Assert.Equal(2, graph.VertexCount);
    }

    [Fact]
    public void VertexComparer_DefaultsToEqualityComparerDefault()
    {
        Assert.Same(EqualityComparer<string>.Default, CreateGraph().VertexComparer);
    }
}
