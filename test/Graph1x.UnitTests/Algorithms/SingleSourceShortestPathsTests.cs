using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class SingleSourceShortestPathsTests
{
    private static DirectedGraph<string, WeightedEdge<string, int>> Directed(
        params (string Source, string Target, int Weight)[] edges)
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, weight) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, weight));
        }

        return graph;
    }

    [Fact]
    public void FindPathsFrom_MatchesPerPairQueries_OnSeededRandomGraph()
    {
        var random = new Random(20260703);
        var graph = new DirectedGraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < 12; v++)
        {
            graph.AddVertex(v);
        }

        for (var i = 0; i < 30; i++)
        {
            graph.AddEdge(new WeightedEdge<int, int>(random.Next(12), random.Next(12), random.Next(1, 20)));
        }

        var dijkstra = new DijkstraShortestPath<int, WeightedEdge<int, int>, int>(e => e.Weight);
        var fromZero = dijkstra.FindPathsFrom(graph, 0);

        foreach (var target in graph.Vertices)
        {
            var perPair = dijkstra.FindPath(graph, 0, target);
            var bulk = fromZero.To(target);

            Assert.Equal(perPair.IsReachable, bulk.IsReachable);
            if (perPair.IsReachable)
            {
                Assert.Equal(perPair.Distance, bulk.Distance);
            }
        }
    }

    [Fact]
    public void To_Source_IsZeroLengthPath()
    {
        var graph = Directed(("a", "b", 1));

        var paths = new DijkstraShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight)
            .FindPathsFrom(graph, "a");

        var result = paths.To("a");

        Assert.Equal(0, result.Distance);
        Assert.Equal(["a"], result.Path);
    }

    [Fact]
    public void To_ReconstructsFullPaths()
    {
        var graph = Directed(("a", "b", 2), ("b", "c", 3), ("a", "c", 10));

        var paths = graph.ShortestPathsFrom("a");

        Assert.Equal(["a", "b", "c"], paths.To("c").Path);
        Assert.Equal(5, paths.To("c").Distance);
    }

    [Fact]
    public void To_UnreachableVertex_ReportsUnreachable()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("island");

        var paths = graph.ShortestPathsFrom("a");

        Assert.False(paths.IsReachable("island"));
        Assert.False(paths.To("island").IsReachable);
        Assert.Throws<InvalidOperationException>(() => paths.To("island").Distance);
    }

    [Fact]
    public void To_UnknownVertex_Throws()
    {
        var graph = Directed(("a", "b", 1));

        var paths = graph.ShortestPathsFrom("a");

        Assert.Throws<ArgumentException>(() => paths.To("ghost"));
        Assert.Throws<ArgumentException>(() => paths.IsReachable("ghost"));
    }

    [Fact]
    public void FindPathsFrom_MissingSource_Throws()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Throws<ArgumentException>(() => graph.ShortestPathsFrom("ghost"));
    }

    [Fact]
    public void Dijkstra_FindPathsFrom_NegativeWeight_Throws()
    {
        var graph = Directed(("a", "b", -1));

        Assert.Throws<NegativeWeightException>(() => graph.ShortestPathsFrom("a"));
    }

    [Fact]
    public void BellmanFord_FindPathsFrom_SupportsNegativeEdges()
    {
        var graph = Directed(("a", "b", 3), ("b", "c", -2), ("a", "c", 2));

        var paths = new BellmanFordShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight)
            .FindPathsFrom(graph, "a");

        Assert.Equal(1, paths.To("c").Distance);
    }

    [Fact]
    public void BellmanFord_FindPathsFrom_NegativeCycle_Throws()
    {
        var graph = Directed(("a", "b", 1), ("b", "a", -2));

        var algorithm = new BellmanFordShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight);

        Assert.Throws<NegativeCycleException>(() => algorithm.FindPathsFrom(graph, "a"));
    }

    [Fact]
    public void Distances_ExposeAllReachedVertices()
    {
        var graph = Directed(("a", "b", 1), ("b", "c", 1));
        graph.AddVertex("island");

        var paths = graph.ShortestPathsFrom("a");

        Assert.Equal(3, paths.Distances.Count); // a, b, c — the island is absent
        Assert.Equal(2, paths.Distances["c"]);
    }

    [Fact]
    public void Facade_WithSelector_WorksOnUnweightedEdges()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var paths = graph.ShortestPathsFrom("a", _ => 1);

        Assert.Equal(2, paths.To("c").Distance);
    }

    [Fact]
    public void Multigraph_UsesCheapestParallelEdge()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 9));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        Assert.Equal(2, graph.ShortestPathsFrom("a").To("b").Distance);
    }

    [Fact]
    public void CustomComparer_IdentifiesVerticesThroughIt()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new WeightedEdge<string, int>("Lisbon", "Porto", 3));

        var paths = graph.ShortestPathsFrom("LISBON");

        Assert.Equal(3, paths.To("porto").Distance);
    }

    [Fact]
    public void Source_IsExposed()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Equal("a", graph.ShortestPathsFrom("a").Source);
    }
}
