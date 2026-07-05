using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>
/// Edmonds-Karp vs Dinic on seeded random capacity networks — the evidence
/// for choosing between the two strategies.
/// </summary>
[MemoryDiagnoser]
public class MaxFlowBenchmarks
{
    private DirectedMultigraph<int, WeightedEdge<int, int>> _network = new();

    [Params(50, 150)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _network = new DirectedMultigraph<int, WeightedEdge<int, int>>();
        for (var v = 0; v < Size; v++)
        {
            _network.AddVertex(v);
        }

        var edgeCount = Size * 6;
        for (var i = 0; i < edgeCount; i++)
        {
            var a = random.Next(Size);
            var b = random.Next(Size);
            if (a != b)
            {
                _network.AddEdge(new WeightedEdge<int, int>(a, b, random.Next(1, 100)));
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int EdmondsKarp()
        => new EdmondsKarpMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMaximumFlow(_network, 0, Size - 1).FlowValue;

    [Benchmark]
    public int Dinic()
        => new DinicMaximumFlow<int, WeightedEdge<int, int>, int>(e => e.Weight)
            .FindMaximumFlow(_network, 0, Size - 1).FlowValue;
}
