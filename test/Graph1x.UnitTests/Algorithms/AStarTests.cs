using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class AStarTests
{
    /// <summary>Builds a 4x4 grid graph with unit weights; vertices are (x, y) tuples.</summary>
    private static UndirectedGraph<(int X, int Y), WeightedEdge<(int X, int Y), int>> Grid()
    {
        var graph = new UndirectedGraph<(int X, int Y), WeightedEdge<(int X, int Y), int>>();
        for (var x = 0; x < 4; x++)
        {
            for (var y = 0; y < 4; y++)
            {
                if (x < 3)
                {
                    graph.AddEdge(new WeightedEdge<(int, int), int>((x, y), (x + 1, y), 1));
                }

                if (y < 3)
                {
                    graph.AddEdge(new WeightedEdge<(int, int), int>((x, y), (x, y + 1), 1));
                }
            }
        }

        return graph;
    }

    /// <summary>Manhattan distance — admissible and consistent on a unit grid.</summary>
    private static int Manhattan((int X, int Y) from, (int X, int Y) to)
        => Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);

    [Fact]
    public void FindPath_WithManhattanHeuristic_FindsOptimalGridPath()
    {
        var algorithm = new AStarShortestPath<(int X, int Y), WeightedEdge<(int X, int Y), int>, int>(
            edge => edge.Weight,
            Manhattan);

        var result = algorithm.FindPath(Grid(), (0, 0), (3, 3));

        Assert.True(result.IsReachable);
        Assert.Equal(6, result.Distance);
        Assert.Equal(7, result.Path.Count);
        Assert.Equal((0, 0), result.Path[0]);
        Assert.Equal((3, 3), result.Path[^1]);
    }

    [Fact]
    public void FindPath_WithZeroHeuristic_MatchesDijkstra()
    {
        var grid = Grid();
        var aStar = new AStarShortestPath<(int X, int Y), WeightedEdge<(int X, int Y), int>, int>(
            edge => edge.Weight,
            (_, _) => 0);
        var dijkstra = new DijkstraShortestPath<(int X, int Y), WeightedEdge<(int X, int Y), int>, int>(
            edge => edge.Weight);

        Assert.Equal(
            dijkstra.FindPath(grid, (0, 0), (2, 3)).Distance,
            aStar.FindPath(grid, (0, 0), (2, 3)).Distance);
    }

    [Fact]
    public void FindPath_UnreachableTarget_ReportsUnreachable()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddVertex("island");

        var algorithm = new AStarShortestPath<string, WeightedEdge<string, int>, int>(
            edge => edge.Weight,
            (_, _) => 0);

        Assert.False(algorithm.FindPath(graph, "a", "island").IsReachable);
    }

    [Fact]
    public void FindPath_NegativeWeight_Throws()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", -1));

        var algorithm = new AStarShortestPath<string, WeightedEdge<string, int>, int>(
            edge => edge.Weight,
            (_, _) => 0);

        Assert.Throws<NegativeWeightException>(() => algorithm.FindPath(graph, "a", "b"));
    }

    [Fact]
    public void FindPath_SourceEqualsTarget_IsZero()
    {
        var algorithm = new AStarShortestPath<(int X, int Y), WeightedEdge<(int X, int Y), int>, int>(
            edge => edge.Weight,
            Manhattan);

        var result = algorithm.FindPath(Grid(), (1, 1), (1, 1));

        Assert.Equal(0, result.Distance);
        Assert.Equal([(1, 1)], result.Path);
    }

    [Fact]
    public void Constructor_NullHeuristic_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AStarShortestPath<string, WeightedEdge<string, int>, int>(edge => edge.Weight, null!));
    }
}
