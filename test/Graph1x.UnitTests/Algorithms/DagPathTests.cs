using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class DagPathTests
{
    private static DirectedGraph<string, WeightedEdge<string, int>> Weighted(
        params (string Source, string Target, int Weight)[] edges)
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        foreach (var (source, target, weight) in edges)
        {
            graph.AddEdge(new WeightedEdge<string, int>(source, target, weight));
        }

        return graph;
    }

    // ---- DagShortestPathsFrom ----

    [Fact]
    public void DagShortestPathsFrom_FindsShortestDistancesAndPaths()
    {
        var graph = Weighted(("a", "b", 1), ("a", "c", 4), ("b", "c", 2), ("c", "d", 1));

        var paths = graph.DagShortestPathsFrom("a");

        Assert.Equal(0, paths.Distances["a"]);
        Assert.Equal(3, paths.Distances["c"]);
        Assert.Equal(4, paths.Distances["d"]);
        Assert.Equal(["a", "b", "c", "d"], paths.To("d").Path);
    }

    [Fact]
    public void DagShortestPathsFrom_SupportsNegativeWeights()
    {
        var graph = Weighted(("a", "b", -5), ("b", "c", 2), ("a", "c", 1));

        var paths = graph.DagShortestPathsFrom("a");

        Assert.Equal(-3, paths.Distances["c"]);
        Assert.Equal(["a", "b", "c"], paths.To("c").Path);
    }

    [Fact]
    public void DagShortestPathsFrom_UnreachableTarget_ReportsNotReachable()
    {
        var graph = Weighted(("a", "b", 1));
        graph.AddVertex("x");

        var paths = graph.DagShortestPathsFrom("a");

        Assert.False(paths.IsReachable("x"));
        Assert.False(paths.To("x").IsReachable);
        Assert.Empty(paths.To("x").Path);
    }

    [Fact]
    public void DagShortestPathsFrom_OnACyclicGraph_Throws()
    {
        var graph = Weighted(("a", "b", 1), ("b", "c", 1), ("c", "a", 1));

        var exception = Assert.Throws<GraphCycleException>(() => graph.DagShortestPathsFrom("a"));
        Assert.NotEmpty(exception.Cycle);
    }

    [Fact]
    public void DagShortestPathsFrom_SingleVertex_HasZeroDistanceToItself()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddVertex("a");

        var paths = graph.DagShortestPathsFrom("a");

        Assert.Equal(0, paths.Distances["a"]);
        Assert.Equal(["a"], paths.To("a").Path);
    }

    [Fact]
    public void DagShortestPathsFrom_ParallelEdges_UseTheCheapest()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 5));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        var paths = graph.DagShortestPathsFrom("a");

        Assert.Equal(2, paths.Distances["b"]);
    }

    [Fact]
    public void DagShortestPathsFrom_WithSelector_WorksOnAnyEdgeType()
    {
        var graph = new DirectedAcyclicGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var paths = graph.DagShortestPathsFrom("a", _ => 1);

        Assert.Equal(2, paths.Distances["c"]);
    }

    [Fact]
    public void DagShortestPathsFrom_UnknownSource_Throws()
    {
        var graph = Weighted(("a", "b", 1));

        Assert.Throws<ArgumentException>(() => graph.DagShortestPathsFrom("x"));
    }

    [Fact]
    public void DagShortestPathsFrom_NullArguments_Throw()
    {
        var graph = Weighted(("a", "b", 1));

        Assert.Throws<ArgumentNullException>(
            () => default(IDirectedGraph<string, WeightedEdge<string, int>>)!.DagShortestPathsFrom("a"));
        Assert.Throws<ArgumentNullException>(
            () => graph.DagShortestPathsFrom("a", default(Func<WeightedEdge<string, int>, int>)!));
    }

    // ---- DagLongestPathsFrom ----

    [Fact]
    public void DagLongestPathsFrom_PicksTheHeaviestPath()
    {
        var graph = Weighted(("a", "b", 1), ("b", "d", 1), ("a", "c", 10), ("c", "d", 1));

        var paths = graph.DagLongestPathsFrom("a");

        Assert.Equal(11, paths.Distances["d"]);
        Assert.Equal(["a", "c", "d"], paths.To("d").Path);
    }

    [Fact]
    public void DagLongestPathsFrom_ParallelEdges_UseTheHeaviest()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 5));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));

        var paths = graph.DagLongestPathsFrom("a");

        Assert.Equal(5, paths.Distances["b"]);
    }

    [Fact]
    public void DagLongestPathsFrom_UnreachableTarget_ReportsNotReachable()
    {
        var graph = Weighted(("a", "b", 1), ("x", "y", 1));

        var paths = graph.DagLongestPathsFrom("a");

        Assert.False(paths.IsReachable("y"));
    }

    [Fact]
    public void DagLongestPathsFrom_OnACyclicGraph_Throws()
    {
        var graph = Weighted(("a", "b", 1), ("b", "a", 1));

        Assert.Throws<GraphCycleException>(() => graph.DagLongestPathsFrom("a"));
    }

    // ---- CriticalPath ----

    [Fact]
    public void CriticalPath_FindsTheLongestPathAnywhereInTheDag()
    {
        var graph = Weighted(
            ("compile", "test", 3),
            ("test", "package", 4),
            ("compile", "package", 5));

        var critical = graph.CriticalPath();

        Assert.True(critical.IsReachable);
        Assert.Equal(7, critical.Distance);
        Assert.Equal(["compile", "test", "package"], critical.Path);
        Assert.Equal("compile", critical.Source);
        Assert.Equal("package", critical.Target);
    }

    [Fact]
    public void CriticalPath_DoesNotHaveToStartAtARoot()
    {
        // Reaching b from a costs -5, so the heaviest path starts mid-graph at b.
        var graph = Weighted(("a", "b", -5), ("b", "c", 10), ("a", "c", 2));

        var critical = graph.CriticalPath();

        Assert.Equal(10, critical.Distance);
        Assert.Equal(["b", "c"], critical.Path);
    }

    [Fact]
    public void CriticalPath_AllNegativeEdges_YieldsASingleVertex()
    {
        var graph = Weighted(("a", "b", -1), ("b", "c", -2));

        var critical = graph.CriticalPath();

        Assert.Equal(0, critical.Distance);
        Assert.Single(critical.Path);
    }

    [Fact]
    public void CriticalPath_TiesBreakByInsertionOrder()
    {
        var graph = Weighted(("a", "b", 1), ("a", "c", 1));

        var critical = graph.CriticalPath();

        Assert.Equal(1, critical.Distance);
        Assert.Equal(["a", "b"], critical.Path);
    }

    [Fact]
    public void CriticalPath_OnAnEmptyGraph_Throws()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();

        Assert.Throws<InvalidOperationException>(() => graph.CriticalPath());
    }

    [Fact]
    public void CriticalPath_OnACyclicGraph_Throws()
    {
        var graph = Weighted(("a", "b", 1), ("b", "a", 1));

        Assert.Throws<GraphCycleException>(() => graph.CriticalPath());
    }

    [Fact]
    public void CriticalPath_WithSelector_WorksOnAnyEdgeType()
    {
        var graph = new DirectedAcyclicGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var critical = graph.CriticalPath(_ => 1);

        Assert.Equal(2, critical.Distance);
        Assert.Equal(["a", "b", "c"], critical.Path);
    }

    [Fact]
    public void CriticalPath_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => default(IDirectedGraph<string, WeightedEdge<string, int>>)!.CriticalPath());
        var graph = Weighted(("a", "b", 1));
        Assert.Throws<ArgumentNullException>(
            () => graph.CriticalPath(default(Func<WeightedEdge<string, int>, int>)!));
    }
}
