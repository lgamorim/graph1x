using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class TransitiveClosureReductionTests
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

    [Fact]
    public void Closure_Chain_AddsShortcutEdges()
    {
        var graph = Directed(("a", "b"), ("b", "c"));

        var closure = graph.TransitiveClosure();

        Assert.Equal(3, closure.EdgeCount);
        Assert.True(closure.ContainsEdge("a", "c"));
    }

    [Fact]
    public void Closure_Diamond_ReachesTheJoin()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        var closure = graph.TransitiveClosure();

        Assert.True(closure.ContainsEdge("a", "d"));
        Assert.Equal(5, closure.EdgeCount);
    }

    [Fact]
    public void Closure_Cycle_ProducesSelfLoops()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        var closure = graph.TransitiveClosure();

        Assert.True(closure.ContainsEdge("a", "a"));
        Assert.True(closure.ContainsEdge("b", "b"));
        Assert.Equal(4, closure.EdgeCount);
    }

    [Fact]
    public void Closure_PreservesIsolatedVertices()
    {
        var graph = Directed(("a", "b"));
        graph.AddVertex("lonely");

        var closure = graph.TransitiveClosure();

        Assert.True(closure.ContainsVertex("lonely"));
        Assert.Equal(3, closure.VertexCount);
    }

    [Fact]
    public void Closure_WithEdgeFactory_BuildsCustomEdges()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 1));

        var closure = graph.TransitiveClosure((source, target) => new WeightedEdge<string, int>(source, target, 0));

        Assert.True(closure.ContainsEdge("a", "c"));
    }

    [Fact]
    public void Closure_AgreesWithBfsReachability_OnSeededRandomGraph()
    {
        var random = new Random(20260703);
        var graph = new DirectedGraph<int, Edge<int>>();
        for (var v = 0; v < 10; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 20; i++)
        {
            graph.AddEdge(new Edge<int>(random.Next(10), random.Next(10)));
        }

        var closure = graph.TransitiveClosure();

        foreach (var source in graph.Vertices)
        {
            // BFS visits exactly the vertices reachable from source; for any
            // target other than source itself that means a path of length >= 1.
            var reachable = graph.BreadthFirstSearch(source).ToHashSet();

            foreach (var target in graph.Vertices.Where(v => v != source))
            {
                Assert.Equal(reachable.Contains(target), closure.ContainsEdge(source, target));
            }
        }
    }

    [Fact]
    public void Reduction_RemovesShortcutEdge()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("a", "c"));

        var reduced = graph.TransitiveReduction();

        Assert.Equal(2, reduced.EdgeCount);
        Assert.False(reduced.ContainsEdge("a", "c"));
    }

    [Fact]
    public void Reduction_Diamond_KeepsAllEdges()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        Assert.Equal(4, graph.TransitiveReduction().EdgeCount);
    }

    [Fact]
    public void Reduction_TransitiveTournament_ReducesToChain()
    {
        var graph = Directed(
            ("a", "b"), ("a", "c"), ("a", "d"),
            ("b", "c"), ("b", "d"),
            ("c", "d"));

        var reduced = graph.TransitiveReduction();

        Assert.Equal(3, reduced.EdgeCount);
        Assert.True(reduced.ContainsEdge("a", "b"));
        Assert.True(reduced.ContainsEdge("b", "c"));
        Assert.True(reduced.ContainsEdge("c", "d"));
    }

    [Fact]
    public void Reduction_CyclicGraph_Throws()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        Assert.Throws<GraphCycleException>(() => graph.TransitiveReduction());
    }

    [Fact]
    public void Reduction_PreservesVertices()
    {
        var graph = Directed(("a", "b"));
        graph.AddVertex("lonely");

        Assert.Equal(3, graph.TransitiveReduction().VertexCount);
    }

    [Fact]
    public void Reduction_ThenClosure_EqualsClosureOfOriginal_OnSeededRandomDag()
    {
        var random = new Random(20260703);
        var graph = new DirectedGraph<int, Edge<int>>();
        for (var v = 0; v < 10; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 25; i++)
        {
            var a = random.Next(10);
            var b = random.Next(10);
            if (a < b)
            {
                graph.AddEdge(new Edge<int>(a, b)); // ascending edges keep it a DAG
            }
        }

        var closureOfOriginal = graph.TransitiveClosure();
        var closureOfReduced = graph.TransitiveReduction().TransitiveClosure();

        Assert.Equal(closureOfOriginal.EdgeCount, closureOfReduced.EdgeCount);
        foreach (var edge in closureOfOriginal.Edges)
        {
            Assert.True(closureOfReduced.ContainsEdge(edge.Source, edge.Target));
        }
    }
}
