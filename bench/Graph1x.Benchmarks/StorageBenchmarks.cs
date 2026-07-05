using BenchmarkDotNet.Attributes;
using Graph1x;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.Benchmarks;

/// <summary>
/// The sparse/dense storage trade-off: adjacency-list vs adjacency-matrix
/// build cost and edge-lookup cost at a fixed density.
/// </summary>
[MemoryDiagnoser]
public class StorageBenchmarks
{
    private List<Edge<int>> _edges = [];
    private UndirectedGraph<int, Edge<int>> _list = new();
    private UndirectedAdjacencyMatrixGraph<int, Edge<int>> _matrix = new();

    [Params(100, 300)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _edges = [.. GraphGenerator.ErdosRenyi(Size, 0.5, seed: 42).Edges];
        _list = Populate(new UndirectedGraph<int, Edge<int>>());
        _matrix = Populate(new UndirectedAdjacencyMatrixGraph<int, Edge<int>>());
    }

    [Benchmark(Baseline = true)]
    public int BuildAdjacencyList() => Populate(new UndirectedGraph<int, Edge<int>>()).EdgeCount;

    [Benchmark]
    public int BuildAdjacencyMatrix() => Populate(new UndirectedAdjacencyMatrixGraph<int, Edge<int>>()).EdgeCount;

    [Benchmark]
    public int LookupsAdjacencyList() => CountPresentPairs(_list);

    [Benchmark]
    public int LookupsAdjacencyMatrix() => CountPresentPairs(_matrix);

    private TGraph Populate<TGraph>(TGraph graph)
        where TGraph : IMutableGraph<int, Edge<int>>
    {
        for (var v = 0; v < Size; v++)
        {
            graph.AddVertex(v);
        }

        foreach (var edge in _edges)
        {
            graph.AddEdge(edge);
        }

        return graph;
    }

    private int CountPresentPairs(IReadOnlyGraph<int, Edge<int>> graph)
    {
        var present = 0;
        for (var i = 0; i < Size; i++)
        {
            for (var j = i + 1; j < Size; j++)
            {
                if (graph.ContainsEdge(i, j))
                {
                    present++;
                }
            }
        }

        return present;
    }
}
