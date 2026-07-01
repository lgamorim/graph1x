using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class MinimumSpanningTreeTests
{
    private static UndirectedGraph<string, WeightedEdge<string, int>> Undirected(
        params (string Source, string Target, int Weight)[] edges)
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, weight) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, weight));
        }

        return graph;
    }

    private static IEnumerable<IMinimumSpanningTreeAlgorithm<string, WeightedEdge<string, int>, int>> Algorithms()
    {
        yield return new KruskalMinimumSpanningTree<string, WeightedEdge<string, int>, int>(e => e.Weight);
        yield return new PrimMinimumSpanningTree<string, WeightedEdge<string, int>, int>(e => e.Weight);
    }

    /// <summary>Asserts the chosen edges span the graph exactly like the original (same components, |V| - #components edges).</summary>
    private static void AssertSpans(
        UndirectedGraph<string, WeightedEdge<string, int>> graph,
        IReadOnlyList<WeightedEdge<string, int>> forest)
    {
        var reconstructed = new UndirectedMultigraph<string, WeightedEdge<string, int>>();
        foreach (var vertex in graph.Vertices)
        {
            reconstructed.AddVertex(vertex);
        }

        foreach (var edge in forest)
        {
            reconstructed.AddEdge(edge);
        }

        var expectedComponents = graph.ConnectedComponents().Count;
        Assert.Equal(expectedComponents, reconstructed.ConnectedComponents().Count);
        Assert.Equal(graph.VertexCount - expectedComponents, forest.Count);
    }

    [Fact]
    public void Mst_ClassicFixture_HasKnownTotalWeight()
    {
        // Weights chosen so the MST is unique: {a-b:1, b-c:2, a-d:3}.
        var graph = Undirected(("a", "b", 1), ("b", "c", 2), ("a", "c", 4), ("a", "d", 3), ("c", "d", 5));

        foreach (var algorithm in Algorithms())
        {
            var forest = algorithm.FindMinimumSpanningForest(graph);

            Assert.Equal(6, forest.Sum(e => e.Weight));
            AssertSpans(graph, forest);
        }
    }

    [Fact]
    public void Mst_TiedWeights_StillProducesValidMinimumForest()
    {
        // Square with all sides weight 1 and one diagonal weight 1: several
        // valid MSTs exist; assert weight + spanning property, not exact edges.
        var graph = Undirected(("a", "b", 1), ("b", "c", 1), ("c", "d", 1), ("d", "a", 1), ("a", "c", 1));

        foreach (var algorithm in Algorithms())
        {
            var forest = algorithm.FindMinimumSpanningForest(graph);

            Assert.Equal(3, forest.Sum(e => e.Weight));
            AssertSpans(graph, forest);
        }
    }

    [Fact]
    public void Mst_DisconnectedGraph_YieldsSpanningForest()
    {
        var graph = Undirected(("a", "b", 1), ("b", "c", 2), ("x", "y", 7));
        graph.AddVertex("lonely");

        foreach (var algorithm in Algorithms())
        {
            var forest = algorithm.FindMinimumSpanningForest(graph);

            Assert.Equal(10, forest.Sum(e => e.Weight));
            AssertSpans(graph, forest); // 6 vertices, 3 components -> 3 edges
        }
    }

    [Fact]
    public void Mst_SelfLoops_AreNeverChosen()
    {
        var graph = Undirected(("a", "a", 0), ("a", "b", 5));

        foreach (var algorithm in Algorithms())
        {
            var forest = algorithm.FindMinimumSpanningForest(graph);

            Assert.Equal([new WeightedEdge<string, int>("a", "b", 5)], forest);
        }
    }

    [Fact]
    public void Mst_ParallelEdges_PickTheCheapest()
    {
        var graph = new UndirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 9));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        var kruskal = new KruskalMinimumSpanningTree<string, WeightedEdge<string, int>, int>(e => e.Weight);
        var prim = new PrimMinimumSpanningTree<string, WeightedEdge<string, int>, int>(e => e.Weight);

        Assert.Equal(2, kruskal.FindMinimumSpanningForest(graph).Sum(e => e.Weight));
        Assert.Equal(2, prim.FindMinimumSpanningForest(graph).Sum(e => e.Weight));
    }

    [Fact]
    public void Mst_NegativeWeights_AreSupported()
    {
        var graph = Undirected(("a", "b", -3), ("b", "c", 2), ("a", "c", 1));

        foreach (var algorithm in Algorithms())
        {
            Assert.Equal(-2, algorithm.FindMinimumSpanningForest(graph).Sum(e => e.Weight));
        }
    }

    [Fact]
    public void Mst_EmptyGraph_YieldsEmptyForest()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();

        foreach (var algorithm in Algorithms())
        {
            Assert.Empty(algorithm.FindMinimumSpanningForest(graph));
        }
    }

    [Fact]
    public void Mst_SingleVertex_YieldsEmptyForest()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddVertex("a");

        foreach (var algorithm in Algorithms())
        {
            Assert.Empty(algorithm.FindMinimumSpanningForest(graph));
        }
    }

    [Fact]
    public void Mst_DirectedGraph_Throws()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));

        foreach (var algorithm in Algorithms())
        {
            Assert.Throws<ArgumentException>(() => algorithm.FindMinimumSpanningForest(graph));
        }
    }

    [Fact]
    public void KruskalAndPrim_AgreeOnSeededRandomGraph()
    {
        var random = new Random(20260702);
        var graph = new UndirectedMultigraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < 15; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 40; i++)
        {
            var a = random.Next(15);
            var b = random.Next(15);
            graph.AddEdge(new WeightedEdge<int, int>(a, b, random.Next(1, 50)));
        }

        var kruskal = new KruskalMinimumSpanningTree<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMinimumSpanningForest(graph);
        var prim = new PrimMinimumSpanningTree<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMinimumSpanningForest(graph);

        Assert.Equal(kruskal.Sum(e => e.Weight), prim.Sum(e => e.Weight));
        Assert.Equal(kruskal.Count, prim.Count);
    }

    [Fact]
    public void Facade_MinimumSpanningForest_UsesWeightedEdgesDirectly()
    {
        var graph = Undirected(("a", "b", 1), ("b", "c", 2), ("a", "c", 4));

        var forest = graph.MinimumSpanningForest();

        Assert.Equal(3, forest.Sum(e => e.Weight));
    }

    [Fact]
    public void Facade_MinimumSpanningForest_WithSelector()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        Assert.Equal(2, graph.MinimumSpanningForest(_ => 1).Count);
    }
}
