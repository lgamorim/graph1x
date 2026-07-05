using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>Full BFS/DFS sweeps over deep and random structures.</summary>
[MemoryDiagnoser]
public class TraversalBenchmarks
{
    private UndirectedGraph<int, Edge<int>> _path = new();
    private UndirectedGraph<int, Edge<int>> _random = new();

    [Params(1_000, 10_000, 100_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _path = GraphGenerator.Path(Size);
        _random = GraphGenerator.ErdosRenyi(Math.Min(Size, 2_000), 8.0 / Math.Min(Size, 2_000), seed: 42);
    }

    [Benchmark]
    public int BfsPath() => _path.BreadthFirstSearch(0).Count();

    [Benchmark]
    public int DfsPath() => _path.DepthFirstSearch(0).Count();

    [Benchmark]
    public int ConnectedComponentsRandom() => _random.ConnectedComponents().Count;
}
