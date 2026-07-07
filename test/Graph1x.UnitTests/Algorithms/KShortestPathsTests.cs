using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class KShortestPathsTests
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

    private static DirectedGraph<string, WeightedEdge<string, int>> ClassicYenGraph()
        => Directed(
            ("c", "d", 3),
            ("c", "e", 2),
            ("d", "f", 4),
            ("e", "d", 1),
            ("e", "f", 2),
            ("e", "g", 3),
            ("f", "g", 2),
            ("f", "h", 1),
            ("g", "h", 2));

    [Fact]
    public void EnumerateShortestPaths_FirstYielded_EqualsDijkstra()
    {
        var graph = ClassicYenGraph();

        var first = graph.EnumerateShortestPaths("c", "h").First();
        var dijkstra = graph.ShortestPath("c", "h");

        Assert.Equal(dijkstra.Distance, first.Distance);
        Assert.Equal(dijkstra.Path, first.Path);
    }

    [Fact]
    public void EnumerateShortestPaths_ClassicYenExample_YieldsKnownRanking()
    {
        var graph = ClassicYenGraph();

        var paths = graph.EnumerateShortestPaths("c", "h").Take(3).ToList();

        Assert.Equal(3, paths.Count);
        Assert.Equal(5, paths[0].Distance);
        Assert.Equal(["c", "e", "f", "h"], paths[0].Path);
        Assert.Equal(7, paths[1].Distance);
        Assert.Equal(["c", "e", "g", "h"], paths[1].Path);
        Assert.Equal(8, paths[2].Distance);
    }

    [Fact]
    public void EnumerateShortestPaths_WeightsAreNondecreasing_AndPathsLoopless()
    {
        var random = GraphGenerator.ErdosRenyiDirected(20, 0.2, seed: 42);
        var graph = new DirectedGraph<int, WeightedEdge<int, int>>();
        foreach (var vertex in random.Vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var edge in random.Edges)
        {
            graph.AddEdge(new WeightedEdge<int, int>(
                edge.Source, edge.Target, (edge.Source * 31 + edge.Target) % 10 + 1));
        }

        var paths = graph.EnumerateShortestPaths(0, 19, e => e.Weight).Take(20).ToList();

        Assert.NotEmpty(paths);
        for (var i = 1; i < paths.Count; i++)
        {
            Assert.True(paths[i - 1].Distance <= paths[i].Distance);
        }

        foreach (var path in paths)
        {
            Assert.Equal(path.Path.Count, path.Path.Distinct().Count()); // loopless
            Assert.Equal(0, path.Path[0]);
            Assert.Equal(19, path.Path[^1]);
        }
    }

    [Fact]
    public void EnumerateShortestPaths_DistinctVertexSequences_NeverRepeat()
    {
        var graph = ClassicYenGraph();

        var paths = graph.EnumerateShortestPaths("c", "h").ToList();

        var keys = paths.Select(p => string.Join(">", p.Path)).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void EnumerateShortestPaths_SmallDiamond_EnumeratesAllLooplessPathsThenStops()
    {
        var graph = Directed(
            ("a", "b", 1),
            ("a", "c", 2),
            ("b", "d", 1),
            ("c", "d", 1));

        var paths = graph.EnumerateShortestPaths("a", "d").ToList();

        Assert.Equal(2, paths.Count);
        Assert.Equal(["a", "b", "d"], paths[0].Path);
        Assert.Equal(["a", "c", "d"], paths[1].Path);
    }

    [Fact]
    public void EnumerateShortestPaths_UnreachableTarget_YieldsNothing()
    {
        var graph = Directed(("a", "b", 1));
        graph.AddVertex("z");

        Assert.Empty(graph.EnumerateShortestPaths("a", "z"));
    }

    [Fact]
    public void EnumerateShortestPaths_SourceEqualsTarget_YieldsOneTrivialPath()
    {
        var graph = Directed(("a", "b", 1), ("b", "a", 1));

        var paths = graph.EnumerateShortestPaths("a", "a").ToList();

        var only = Assert.Single(paths);
        Assert.Equal(0, only.Distance);
        Assert.Equal(["a"], only.Path);
    }

    [Fact]
    public void EnumerateShortestPaths_UndirectedGraph_TreatsEdgesBothWays()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "c", 5));

        var paths = graph.EnumerateShortestPaths("a", "c").Take(2).ToList();

        Assert.Equal(["a", "b", "c"], paths[0].Path);
        Assert.Equal(2, paths[0].Distance);
        Assert.Equal(["a", "c"], paths[1].Path);
        Assert.Equal(5, paths[1].Distance);
    }

    [Fact]
    public void EnumerateShortestPaths_ParallelEdges_UseCheapestWithoutDuplicatingVertexPaths()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 3));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));

        var paths = graph.EnumerateShortestPaths("a", "b").ToList();

        var only = Assert.Single(paths);
        Assert.Equal(1, only.Distance);
    }

    [Fact]
    public void EnumerateShortestPaths_IsDeterministic()
    {
        var graph = ClassicYenGraph();

        var firstRun = graph.EnumerateShortestPaths("c", "h").Select(p => string.Join(">", p.Path)).ToList();
        var secondRun = graph.EnumerateShortestPaths("c", "h").Select(p => string.Join(">", p.Path)).ToList();

        Assert.Equal(firstRun, secondRun);
    }

    [Fact]
    public void EnumerateShortestPaths_IsLazy_FirstPathOnLargeGraphIsCheap()
    {
        var random = GraphGenerator.BarabasiAlbert(2000, 3, seed: 11);
        var graph = new UndirectedGraph<int, WeightedEdge<int, int>>();
        foreach (var vertex in random.Vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var edge in random.Edges)
        {
            graph.AddEdge(new WeightedEdge<int, int>(edge.Source, edge.Target, 1));
        }

        var first = graph.EnumerateShortestPaths(0, 1999, e => e.Weight).First();

        Assert.True(first.IsReachable);
    }

    [Fact]
    public void EnumerateShortestPaths_NegativeWeight_ThrowsOnEnumeration()
    {
        var graph = Directed(("a", "b", -1));

        var sequence = graph.EnumerateShortestPaths("a", "b");

        Assert.Throws<NegativeWeightException>(() => sequence.First());
    }

    [Fact]
    public void EnumerateShortestPaths_InvalidArguments_ThrowEagerly()
    {
        var graph = Directed(("a", "b", 1));

        Assert.Throws<ArgumentNullException>(
            () => GraphKShortestPathsExtensions.EnumerateShortestPaths<string, WeightedEdge<string, int>, int>(
                null!, "a", "b", e => e.Weight));
        Assert.Throws<ArgumentNullException>(() => graph.EnumerateShortestPaths("a", "b", (Func<WeightedEdge<string, int>, int>)null!));
        Assert.Throws<ArgumentException>(() => graph.EnumerateShortestPaths("missing", "b"));
        Assert.Throws<ArgumentException>(() => graph.EnumerateShortestPaths("a", "missing"));
    }

    [Fact]
    public void EnumerateShortestPaths_SelectorOverload_MatchesConvenienceOverload()
    {
        var graph = ClassicYenGraph();

        var viaSelector = graph.EnumerateShortestPaths("c", "h", e => e.Weight).Take(3).Select(p => p.Distance);
        var viaConvenience = graph.EnumerateShortestPaths("c", "h").Take(3).Select(p => p.Distance);

        Assert.Equal(viaConvenience, viaSelector);
    }
}
