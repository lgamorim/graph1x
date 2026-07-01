using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class CycleDetectionTests
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

    /// <summary>Asserts the returned cycle is genuine: consecutive vertices (and last-to-first) are connected.</summary>
    private static void AssertValidCycle<TEdge>(IReadOnlyGraph<string, TEdge> graph, IReadOnlyList<string> cycle)
        where TEdge : Graph1x.Edges.IEdge<string>
    {
        Assert.NotEmpty(cycle);
        for (var i = 0; i < cycle.Count; i++)
        {
            var next = cycle[(i + 1) % cycle.Count];
            Assert.True(graph.ContainsEdge(cycle[i], next), $"expected edge {cycle[i]} -> {next}");
        }
    }

    [Fact]
    public void EmptyGraph_HasNoCycle()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.False(graph.HasCycle());
        Assert.Null(graph.FindCycle());
    }

    [Fact]
    public void SingleVertex_HasNoCycle()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void Directed_Diamond_HasNoCycle()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        Assert.False(graph.HasCycle());
        Assert.Null(graph.FindCycle());
    }

    [Fact]
    public void Directed_SelfLoop_IsACycle()
    {
        var graph = Directed(("a", "a"));

        Assert.True(graph.HasCycle());
        Assert.Equal(["a"], graph.FindCycle());
    }

    [Fact]
    public void Directed_TwoNodeCycle_IsFound()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        var cycle = graph.FindCycle();

        Assert.NotNull(cycle);
        Assert.Equal(2, cycle.Count);
        AssertValidCycle(graph, cycle);
    }

    [Fact]
    public void Directed_LongCycle_IsFound()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "d"), ("d", "e"), ("e", "a"));

        var cycle = graph.FindCycle();

        Assert.NotNull(cycle);
        Assert.Equal(5, cycle.Count);
        AssertValidCycle(graph, cycle);
    }

    [Fact]
    public void Directed_OppositeEdgesBetweenDifferentPairs_NoCycle()
    {
        // a->b and c->b: reaching b twice via different paths is not a cycle.
        var graph = Directed(("a", "b"), ("c", "b"), ("a", "c"));

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void Directed_CycleInSecondComponent_IsFound()
    {
        var graph = Directed(("a", "b"), ("x", "y"), ("y", "x"));

        var cycle = graph.FindCycle();

        Assert.NotNull(cycle);
        AssertValidCycle(graph, cycle);
        Assert.Contains("x", cycle);
    }

    [Fact]
    public void Undirected_Tree_HasNoCycle()
    {
        var graph = Undirected(("a", "b"), ("a", "c"), ("b", "d"));

        Assert.False(graph.HasCycle());
        Assert.Null(graph.FindCycle());
    }

    [Fact]
    public void Undirected_SingleEdge_IsNotACycle()
    {
        var graph = Undirected(("a", "b"));

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void Undirected_Triangle_IsACycle()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        var cycle = graph.FindCycle();

        Assert.NotNull(cycle);
        Assert.Equal(3, cycle.Count);
        AssertValidCycle(graph, cycle);
    }

    [Fact]
    public void Undirected_SelfLoop_IsACycle()
    {
        var graph = Undirected(("a", "a"));

        Assert.Equal(["a"], graph.FindCycle());
    }

    [Fact]
    public void Undirected_ParallelEdges_FormACycle()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.True(graph.HasCycle());
    }

    [Fact]
    public void Undirected_SingleEdgeInMultigraph_IsNotACycle()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void DirectedAcyclicGraph_NeverReportsACycle()
    {
        var dag = new DirectedAcyclicGraph<string, Edge<string>>();
        dag.AddEdge(new Edge<string>("a", "b"));
        dag.AddEdge(new Edge<string>("b", "c"));
        dag.AddEdge(new Edge<string>("c", "a")); // rejected by the DAG

        Assert.False(dag.HasCycle());
    }
}
