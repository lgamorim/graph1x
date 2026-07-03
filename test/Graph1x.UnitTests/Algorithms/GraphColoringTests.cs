using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class GraphColoringTests
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

    /// <summary>Asserts the coloring is proper (no edge joins same-colored vertices) and uses contiguous colors from 0.</summary>
    private static void AssertProperColoring<TEdge>(
        IReadOnlyGraph<string, TEdge> graph,
        GraphColoring<string> coloring)
        where TEdge : Graph1x.Edges.IEdge<string>
    {
        Assert.Equal(graph.VertexCount, coloring.Colors.Count);
        foreach (var edge in graph.Edges)
        {
            Assert.NotEqual(coloring.ColorOf(edge.Source), coloring.ColorOf(edge.Target));
        }

        var used = coloring.Colors.Values.Distinct().OrderBy(color => color).ToList();
        Assert.Equal(coloring.ColorCount, used.Count);
        if (used.Count > 0)
        {
            Assert.Equal(0, used[0]);
            Assert.Equal(used.Count - 1, used[^1]); // contiguous from zero
        }
    }

    [Fact]
    public void EmptyGraph_UsesNoColors()
    {
        var coloring = new UndirectedGraph<string, Edge<string>>().ColorVertices();

        Assert.Equal(0, coloring.ColorCount);
        Assert.Empty(coloring.Colors);
    }

    [Fact]
    public void IsolatedVertices_UseOneColor()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        var coloring = graph.ColorVertices();

        Assert.Equal(1, coloring.ColorCount);
        Assert.Equal(0, coloring.ColorOf("a"));
        Assert.Equal(0, coloring.ColorOf("b"));
    }

    [Fact]
    public void EvenCycle_UsesTwoColors()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"), ("d", "a"));

        var coloring = graph.ColorVertices();

        Assert.Equal(2, coloring.ColorCount);
        AssertProperColoring(graph, coloring);
    }

    [Fact]
    public void OddCycle_UsesThreeColors()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        var coloring = graph.ColorVertices();

        Assert.Equal(3, coloring.ColorCount);
        AssertProperColoring(graph, coloring);
    }

    [Fact]
    public void CompleteGraphK4_UsesFourColors()
    {
        var graph = Undirected(
            ("a", "b"), ("a", "c"), ("a", "d"), ("b", "c"), ("b", "d"), ("c", "d"));

        var coloring = graph.ColorVertices();

        Assert.Equal(4, coloring.ColorCount);
        AssertProperColoring(graph, coloring);
    }

    [Fact]
    public void BipartiteGraph_DSaturIsExact_TwoColors()
    {
        // DSatur is exact on bipartite graphs; a plain greedy order can need 3+
        // on crown-like fixtures.
        var graph = Undirected(
            ("l1", "r2"), ("l1", "r3"), ("l2", "r1"), ("l2", "r3"), ("l3", "r1"), ("l3", "r2"));

        var coloring = graph.ColorVertices();

        Assert.Equal(2, coloring.ColorCount);
        AssertProperColoring(graph, coloring);
    }

    [Fact]
    public void SeededRandomGraph_ColoringIsAlwaysProper()
    {
        var random = new Random(20260703);
        var graph = new UndirectedGraph<int, Edge<int>>();
        for (var v = 0; v < 20; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 60; i++)
        {
            var a = random.Next(20);
            var b = random.Next(20);
            if (a != b)
            {
                graph.AddEdge(new Edge<int>(a, b));
            }
        }

        var coloring = graph.ColorVertices();

        foreach (var edge in graph.Edges)
        {
            Assert.NotEqual(coloring.ColorOf(edge.Source), coloring.ColorOf(edge.Target));
        }
    }

    [Fact]
    public void ParallelEdges_DoNotAffectTheResult()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Equal(2, graph.ColorVertices().ColorCount);
    }

    [Fact]
    public void SelfLoop_Throws()
    {
        var graph = Undirected(("a", "a"));

        Assert.Throws<ArgumentException>(() => graph.ColorVertices());
    }

    [Fact]
    public void DirectedGraph_IgnoresDirection()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "a"));

        var coloring = graph.ColorVertices();

        Assert.Equal(2, coloring.ColorCount);
        Assert.NotEqual(coloring.ColorOf("a"), coloring.ColorOf("b"));
    }

    [Fact]
    public void CustomComparer_IdentifiesVerticesThroughIt()
    {
        var graph = new UndirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("Lisbon", "Porto"));

        var coloring = graph.ColorVertices();

        Assert.NotEqual(coloring.ColorOf("LISBON"), coloring.ColorOf("porto"));
    }

    [Fact]
    public void ColorOf_UnknownVertex_Throws()
    {
        var graph = Undirected(("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.ColorVertices().ColorOf("ghost"));
    }
}
