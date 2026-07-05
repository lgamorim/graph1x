using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>Kruskal vs Prim across densities — the classic crossover question.</summary>
[MemoryDiagnoser]
public class MstBenchmarks
{
    private UndirectedMultigraph<int, WeightedEdge<int, int>> _graph = new();

    [Params(0.05, 0.5)]
    public double Density { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        const int size = 300;
        var random = new Random(42);
        _graph = new UndirectedMultigraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < size; v++)
        {
            _graph.AddVertex(v);
        }

        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j++)
            {
                if (random.NextDouble() < Density)
                {
                    _graph.AddEdge(new WeightedEdge<int, int>(i, j, random.Next(1, 1000)));
                }
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int Kruskal()
        => new KruskalMinimumSpanningTree<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMinimumSpanningForest(_graph).Count;

    [Benchmark]
    public int Prim()
        => new PrimMinimumSpanningTree<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMinimumSpanningForest(_graph).Count;
}
