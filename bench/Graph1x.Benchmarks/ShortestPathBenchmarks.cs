using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>
/// The single-pair vs single-source trap, and the A* heuristic payoff, on a
/// unit-weight grid.
/// </summary>
[MemoryDiagnoser]
public class ShortestPathBenchmarks
{
    private const int Width = 50;
    private UndirectedGraph<int, Edge<int>> _grid = new();
    private int[] _targets = [];

    [GlobalSetup]
    public void Setup()
    {
        _grid = GraphGenerator.Grid(Width, Width);
        _targets = [.. _grid.Vertices];
    }

    [Benchmark(Baseline = true)]
    public int PairQueryPerTarget()
    {
        var reached = 0;
        foreach (var target in _targets)
        {
            if (_grid.ShortestPath(0, target, _ => 1).IsReachable)
            {
                reached++;
            }
        }

        return reached;
    }

    [Benchmark]
    public int SingleSourceThenLookups()
    {
        var paths = _grid.ShortestPathsFrom(0, _ => 1);
        return _targets.Count(paths.IsReachable);
    }

    [Benchmark]
    public int DijkstraCornerToCorner()
        => _grid.ShortestPath(0, (Width * Width) - 1, _ => 1).Distance;

    [Benchmark]
    public int AStarManhattanCornerToCorner()
    {
        var algorithm = new AStarShortestPath<int, Edge<int>, int>(
            _ => 1,
            (vertex, goal) => Math.Abs((vertex % Width) - (goal % Width)) + Math.Abs((vertex / Width) - (goal / Width)));
        return algorithm.FindPath(_grid, 0, (Width * Width) - 1).Distance;
    }
}
