using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>
/// Sequential vs parallel per-source analysis (Brandes betweenness and
/// closeness) on a scale-free graph — the workload the ParallelOptions
/// overloads exist for.
/// </summary>
[MemoryDiagnoser]
public class ParallelAnalysisBenchmarks
{
    private static readonly ParallelOptions AllCores = new();

    private UndirectedGraph<int, Edge<int>> _graph = new();

    [Params(500, 2_000)]
    public int VertexCount { get; set; }

    [GlobalSetup]
    public void Setup() => _graph = GraphGenerator.BarabasiAlbert(VertexCount, edgesPerNewVertex: 3, seed: 42);

    [Benchmark(Baseline = true)]
    public double BetweennessSequential() => _graph.BetweennessCentrality().Values.Max();

    [Benchmark]
    public double BetweennessParallel() => _graph.BetweennessCentrality(AllCores).Values.Max();

    [Benchmark]
    public double ClosenessSequential() => _graph.ClosenessCentrality().Values.Max();

    [Benchmark]
    public double ClosenessParallel() => _graph.ClosenessCentrality(AllCores).Values.Max();
}
