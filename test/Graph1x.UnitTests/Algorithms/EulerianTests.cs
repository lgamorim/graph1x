using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class EulerianTests
{
    private static UndirectedGraph<string, Edge<string>> Undirected(params (string, string)[] edges)
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    private static DirectedGraph<string, Edge<string>> Directed(params (string, string)[] edges)
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    /// <summary>Asserts the trail uses every edge exactly once and is connected end to end.</summary>
    private static void AssertValidTrail<TEdge>(
        IReadOnlyGraph<string, TEdge> graph,
        IReadOnlyList<TEdge> trail,
        bool mustClose)
        where TEdge : Graph1x.Edges.IEdge<string>
    {
        Assert.Equal(graph.EdgeCount, trail.Count);
        if (trail.Count == 0)
        {
            return;
        }

        if (graph.IsDirected)
        {
            for (var i = 1; i < trail.Count; i++)
            {
                Assert.Equal(trail[i - 1].Target, trail[i].Source);
            }

            if (mustClose)
            {
                Assert.Equal(trail[^1].Target, trail[0].Source);
            }

            return;
        }

        // Undirected: walk the trail, moving across each edge from whichever
        // endpoint we are currently standing on.
        foreach (var start in new[] { trail[0].Source, trail[0].Target })
        {
            var current = start;
            var valid = true;
            foreach (var edge in trail)
            {
                if (edge.Source == current)
                {
                    current = edge.Target;
                }
                else if (edge.Target == current)
                {
                    current = edge.Source;
                }
                else
                {
                    valid = false;
                    break;
                }
            }

            if (valid && (!mustClose || current == start))
            {
                return; // one orientation works
            }
        }

        Assert.Fail("The trail is not connected end to end.");
    }

    [Fact]
    public void Undirected_Triangle_HasEulerianCircuit()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        Assert.True(graph.HasEulerianCircuit());
        Assert.True(graph.HasEulerianPath());

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        AssertValidTrail(graph, circuit, mustClose: true);
    }

    [Fact]
    public void Koenigsberg_HasNoEulerianPathOrCircuit()
    {
        // The classic seven bridges: four odd-degree land masses.
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("north", "island"));
        graph.AddEdge(new Edge<string>("north", "island"));
        graph.AddEdge(new Edge<string>("south", "island"));
        graph.AddEdge(new Edge<string>("south", "island"));
        graph.AddEdge(new Edge<string>("north", "east"));
        graph.AddEdge(new Edge<string>("south", "east"));
        graph.AddEdge(new Edge<string>("island", "east"));

        Assert.False(graph.HasEulerianCircuit());
        Assert.False(graph.HasEulerianPath());
        Assert.Null(graph.FindEulerianCircuit());
        Assert.Null(graph.FindEulerianPath());
    }

    [Fact]
    public void Undirected_PathGraph_HasPathButNoCircuit()
    {
        var graph = Undirected(("a", "b"), ("b", "c"));

        Assert.False(graph.HasEulerianCircuit());
        Assert.True(graph.HasEulerianPath());

        var trail = graph.FindEulerianPath();

        Assert.NotNull(trail);
        AssertValidTrail(graph, trail, mustClose: false);
    }

    [Fact]
    public void FigureEight_TwoTrianglesSharingAVertex_HasCircuit()
    {
        var graph = Undirected(
            ("a", "b"), ("b", "m"), ("m", "a"),
            ("m", "x"), ("x", "y"), ("y", "m"));

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        AssertValidTrail(graph, circuit, mustClose: true);
    }

    [Fact]
    public void DisconnectedEdgeComponents_HaveNoEulerianTrail()
    {
        // Two edge-bearing components ({a,b,c} and {x,y}), each individually
        // traversable — but no single trail covers both.
        var graph = Undirected(("a", "b"), ("b", "c"), ("x", "y"));

        Assert.False(graph.HasEulerianPath());
        Assert.Null(graph.FindEulerianPath());
    }

    [Fact]
    public void IsolatedVertices_AreTolerated()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));
        graph.AddVertex("lonely");

        Assert.True(graph.HasEulerianCircuit());
    }

    [Fact]
    public void EdgelessGraph_HasTrivialCircuitAndPath()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.True(graph.HasEulerianCircuit());
        Assert.True(graph.HasEulerianPath());
        Assert.Equal([], graph.FindEulerianCircuit());
        Assert.Equal([], graph.FindEulerianPath());
    }

    [Fact]
    public void SingleSelfLoop_IsACircuit()
    {
        var graph = Undirected(("a", "a"));

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        Assert.Single(circuit);
    }

    [Fact]
    public void TwoParallelEdges_FormACircuit()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        Assert.Equal(2, circuit.Count);
        AssertValidTrail(graph, circuit, mustClose: true);
    }

    [Fact]
    public void DirectedCycle_HasCircuit()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "a"));

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        AssertValidTrail(graph, circuit, mustClose: true);
    }

    [Fact]
    public void DirectedChain_HasPathOnly()
    {
        var graph = Directed(("a", "b"), ("b", "c"));

        Assert.False(graph.HasEulerianCircuit());
        Assert.True(graph.HasEulerianPath());

        var trail = graph.FindEulerianPath();

        Assert.NotNull(trail);
        Assert.Equal("a", trail[0].Source);
        AssertValidTrail(graph, trail, mustClose: false);
    }

    [Fact]
    public void DirectedUnbalanced_HasNeither()
    {
        var graph = Directed(("a", "b"), ("a", "c"));

        Assert.False(graph.HasEulerianCircuit());
        Assert.False(graph.HasEulerianPath());
    }

    [Fact]
    public void DirectedGraph_ReversedEdgePair_IsACircuit()
    {
        var graph = Directed(("a", "b"), ("b", "a"));

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        AssertValidTrail(graph, circuit, mustClose: true);
    }

    [Fact]
    public void SeededRandomClosedWalk_IsAlwaysEulerian()
    {
        // A multigraph built from a closed random walk is Eulerian by
        // construction: every edge is walked exactly once.
        var random = new Random(20260703);
        var graph = new DirectedMultigraph<int, Edge<int>>();
        var walk = new List<int> { 0 };
        for (var i = 0; i < 60; i++)
        {
            walk.Add(random.Next(8));
        }

        walk.Add(0);
        for (var i = 1; i < walk.Count; i++)
        {
            graph.AddEdge(new Edge<int>(walk[i - 1], walk[i]));
        }

        Assert.True(graph.HasEulerianCircuit());

        var circuit = graph.FindEulerianCircuit();

        Assert.NotNull(circuit);
        Assert.Equal(graph.EdgeCount, circuit.Count);
        for (var i = 1; i < circuit.Count; i++)
        {
            Assert.Equal(circuit[i - 1].Target, circuit[i].Source);
        }
    }
}
