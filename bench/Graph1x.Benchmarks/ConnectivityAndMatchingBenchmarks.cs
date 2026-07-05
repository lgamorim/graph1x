using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>Tarjan SCC on random digraphs and Hopcroft-Karp on random bipartite graphs.</summary>
[MemoryDiagnoser]
public class ConnectivityAndMatchingBenchmarks
{
    private DirectedGraph<int, Edge<int>> _digraph = new();
    private UndirectedGraph<string, Edge<string>> _bipartite = new();

    [GlobalSetup]
    public void Setup()
    {
        _digraph = GraphGenerator.ErdosRenyiDirected(1_000, 0.004, seed: 42);

        var random = new Random(42);
        _bipartite = new UndirectedGraph<string, Edge<string>>();
        for (var i = 0; i < 200; i++)
        {
            _bipartite.AddVertex($"l{i}");
            _bipartite.AddVertex($"r{i}");
        }

        for (var i = 0; i < 2_000; i++)
        {
            _bipartite.AddEdge(new Edge<string>($"l{random.Next(200)}", $"r{random.Next(200)}"));
        }
    }

    [Benchmark]
    public int TarjanScc() => _digraph.StronglyConnectedComponents().Count;

    [Benchmark]
    public int HopcroftKarpMatching() => _bipartite.MaximumBipartiteMatching().Count;
}
