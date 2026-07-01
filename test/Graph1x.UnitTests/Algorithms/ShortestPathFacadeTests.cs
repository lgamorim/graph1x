using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class ShortestPathFacadeTests
{
    [Fact]
    public void ShortestPath_WithWeightSelector_UsesDijkstra()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        // Unweighted edges: constant weight 1 turns Dijkstra into hop counting.
        var result = graph.ShortestPath("a", "c", _ => 1);

        Assert.Equal(2, result.Distance);
        Assert.Equal(["a", "b", "c"], result.Path);
    }

    [Fact]
    public void ShortestPath_OnWeightedEdges_NeedsNoSelector()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 1.5));
        graph.AddEdge(new WeightedEdge<string, double>("b", "c", 2.5));

        Assert.Equal(4.0, graph.ShortestPath("a", "c").Distance);
    }

    [Fact]
    public void Strategies_AreInterchangeable_ThroughTheCommonInterface()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2));
        graph.AddEdge(new WeightedEdge<string, int>("b", "c", 2));

        IShortestPathAlgorithm<string, WeightedEdge<string, int>, int>[] strategies =
        [
            new DijkstraShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight),
            new BellmanFordShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight),
            new AStarShortestPath<string, WeightedEdge<string, int>, int>(e => e.Weight, (_, _) => 0),
        ];

        Assert.All(strategies, strategy => Assert.Equal(4, strategy.FindPath(graph, "a", "c").Distance));
    }

    [Fact]
    public void NegativeWeightException_And_NegativeCycleException_HaveStandardConstructors()
    {
        Assert.NotNull(new NegativeWeightException());
        Assert.Equal("w", new NegativeWeightException("w").Message);
        Assert.NotNull(new NegativeWeightException("w", new InvalidOperationException()).InnerException);

        Assert.NotNull(new NegativeCycleException());
        Assert.Equal("c", new NegativeCycleException("c").Message);
        Assert.NotNull(new NegativeCycleException("c", new InvalidOperationException()).InnerException);
    }
}
