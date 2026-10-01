using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class BiconnectedComponentsTests
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

    private static HashSet<string> VerticesOf(IReadOnlyList<Edge<string>> component)
    {
        var vertices = new HashSet<string>();
        foreach (var edge in component)
        {
            vertices.Add(edge.Source);
            vertices.Add(edge.Target);
        }

        return vertices;
    }

    [Fact]
    public void BiconnectedComponents_EmptyGraph_YieldsNothing()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.BiconnectedComponents());
    }

    [Fact]
    public void BiconnectedComponents_SingleVertex_YieldsNothing()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        Assert.Empty(graph.BiconnectedComponents());
    }

    [Fact]
    public void BiconnectedComponents_SingleEdge_IsOneSingletonComponent()
    {
        var graph = Undirected(("a", "b"));

        var components = graph.BiconnectedComponents();

        var component = Assert.Single(components);
        Assert.Equal([new Edge<string>("a", "b")], component);
    }

    [Fact]
    public void BiconnectedComponents_Cycle_IsOneComponentWithAllEdges()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        var components = graph.BiconnectedComponents();

        var component = Assert.Single(components);
        Assert.Equal(3, component.Count);
    }

    [Fact]
    public void BiconnectedComponents_Path_EachEdgeIsItsOwnComponent()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "d"));

        var components = graph.BiconnectedComponents();

        Assert.Equal(3, components.Count);
        Assert.All(components, component => Assert.Single(component));
    }

    [Fact]
    public void BiconnectedComponents_TriangleWithTail_SplitsAtTheArticulationPoint()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"), ("c", "d"));

        var components = graph.BiconnectedComponents();

        Assert.Equal(2, components.Count);
        var sizes = components.Select(c => c.Count).OrderBy(size => size).ToList();
        Assert.Equal([1, 3], sizes);
    }

    [Fact]
    public void BiconnectedComponents_TwoTrianglesSharingAVertex_ShareVertexAppearsInBoth()
    {
        var graph = Undirected(
            ("a", "b"), ("b", "m"), ("m", "a"),
            ("m", "x"), ("x", "y"), ("y", "m"));

        var components = graph.BiconnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.All(components, component => Assert.Equal(3, component.Count));
        Assert.All(components, component => Assert.Contains("m", VerticesOf(component)));
    }

    [Fact]
    public void BiconnectedComponents_ParallelEdges_FormOneComponent()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var components = graph.BiconnectedComponents();

        Assert.Equal(2, components.Count);
        var sizes = components.Select(c => c.Count).OrderBy(size => size).ToList();
        Assert.Equal([1, 2], sizes);
    }

    [Fact]
    public void BiconnectedComponents_ParallelEdgesWithEndpointSpecificOrdering_KeepsBothEdges()
    {
        // IReadOnlyGraph promises no cross-endpoint ordering: a custom graph
        // may list b's incident edges in a different order than a's. The
        // parent skip must then match the tree edge itself, not "any edge back
        // to the parent", or the tree edge is counted twice and its twin lost.
        var inner = new UndirectedMultigraph<string, IdentityEdge>();
        var first = new IdentityEdge("a", "b");
        var second = new IdentityEdge("a", "b");
        var bridge = new IdentityEdge("b", "c");
        inner.AddEdge(first);
        inner.AddEdge(second);
        inner.AddEdge(bridge);
        var graph = new ReversedIncidenceGraph(inner, "b");

        var components = graph.BiconnectedComponents();

        Assert.Equal(2, components.Count);
        var cycle = Assert.Single(components, component => component.Count == 2);
        Assert.Contains(first, cycle);
        Assert.Contains(second, cycle);
    }

    [Fact]
    public void BiconnectedComponents_SelfLoops_AppearInNoComponent()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        var components = graph.BiconnectedComponents();

        var component = Assert.Single(components);
        Assert.Equal([new Edge<string>("a", "b")], component);
    }

    [Fact]
    public void BiconnectedComponents_DirectedGraph_ThrowsArgumentException()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.BiconnectedComponents());
    }

    [Fact]
    public void BiconnectedComponents_DeepChain_DoesNotOverflowStack()
    {
        var graph = new UndirectedGraph<int, Edge<int>>();
        for (var i = 0; i < 100_000; i++)
        {
            graph.AddEdge(new Edge<int>(i, i + 1));
        }

        Assert.Equal(100_000, graph.BiconnectedComponents().Count);
    }

    [Fact]
    public void BiconnectedComponents_RandomGraph_PartitionsEveryNonLoopEdgeExactlyOnce()
    {
        var graph = GraphGenerator.ErdosRenyi(60, 0.05, seed: 42);

        var components = graph.BiconnectedComponents();

        var assigned = components.Sum(component => component.Count);
        var nonLoopEdges = graph.Edges.Count(edge => edge.Source != edge.Target);
        Assert.Equal(nonLoopEdges, assigned);
    }

    [Fact]
    public void BiconnectedComponents_RandomGraph_BridgesAreExactlyTheSingletonComponents()
    {
        var graph = GraphGenerator.ErdosRenyi(60, 0.05, seed: 7);

        var singletons = graph.BiconnectedComponents()
            .Where(component => component.Count == 1)
            .Select(component => component[0])
            .ToHashSet();

        Assert.Equal(graph.FindBridges().ToHashSet(), singletons);
    }

    [Fact]
    public void BiconnectedComponents_RandomGraph_ArticulationPointsAreVerticesInMultipleComponents()
    {
        var graph = GraphGenerator.ErdosRenyi(60, 0.06, seed: 11);

        var components = graph.BiconnectedComponents();

        var membership = new Dictionary<int, int>();
        foreach (var component in components)
        {
            var vertices = new HashSet<int>();
            foreach (var edge in component)
            {
                vertices.Add(edge.Source);
                vertices.Add(edge.Target);
            }

            foreach (var vertex in vertices)
            {
                membership[vertex] = membership.TryGetValue(vertex, out var count) ? count + 1 : 1;
            }
        }

        var inMultiple = membership.Where(pair => pair.Value >= 2).Select(pair => pair.Key).ToHashSet();
        Assert.Equal(graph.FindArticulationPoints().ToHashSet(), inMultiple);
    }

    /// <summary>An edge with reference identity, so parallel copies stay distinguishable.</summary>
    private sealed class IdentityEdge(string source, string target) : IEdge<string>
    {
        public string Source { get; } = source;

        public string Target { get; } = target;
    }

    /// <summary>
    /// Read-only view that enumerates one vertex's incident edges in reverse,
    /// modelling a graph whose per-vertex storage has no shared ordering.
    /// </summary>
    private sealed class ReversedIncidenceGraph(UndirectedMultigraph<string, IdentityEdge> inner, string reversedVertex)
        : IReadOnlyGraph<string, IdentityEdge>
    {
        public int VertexCount => inner.VertexCount;

        public int EdgeCount => inner.EdgeCount;

        public bool IsDirected => inner.IsDirected;

        public bool AllowsParallelEdges => inner.AllowsParallelEdges;

        public IEqualityComparer<string> VertexComparer => inner.VertexComparer;

        public IEnumerable<string> Vertices => inner.Vertices;

        public IEnumerable<IdentityEdge> Edges => inner.Edges;

        public bool ContainsVertex(string vertex) => inner.ContainsVertex(vertex);

        public bool ContainsEdge(string source, string target) => inner.ContainsEdge(source, target);

        public int Degree(string vertex) => inner.Degree(vertex);

        public IEnumerable<IdentityEdge> AdjacentEdges(string vertex)
            => vertex == reversedVertex ? inner.AdjacentEdges(vertex).Reverse() : inner.AdjacentEdges(vertex);
    }

    [Fact]
    public void TwoEdgeConnectedComponents_EmptyGraph_YieldsNothing()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.TwoEdgeConnectedComponents());
    }

    [Fact]
    public void TwoEdgeConnectedComponents_SingleVertex_IsASingletonComponent()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        var component = Assert.Single(graph.TwoEdgeConnectedComponents());
        Assert.True(component.SetEquals(["a"]));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_SingleEdge_SplitsIntoTwoSingletons()
    {
        var graph = Undirected(("a", "b"));

        var components = graph.TwoEdgeConnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.All(components, component => Assert.Single(component));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_Cycle_IsOneComponent()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"));

        var component = Assert.Single(graph.TwoEdgeConnectedComponents());
        Assert.True(component.SetEquals(["a", "b", "c"]));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_TriangleWithTail_TailVertexIsItsOwnComponent()
    {
        var graph = Undirected(("a", "b"), ("b", "c"), ("c", "a"), ("c", "d"));

        var components = graph.TwoEdgeConnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.Contains(components, component => component.SetEquals(["a", "b", "c"]));
        Assert.Contains(components, component => component.SetEquals(["d"]));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_TwoTrianglesSharingAVertex_StayOneComponent()
    {
        var graph = Undirected(
            ("a", "b"), ("b", "m"), ("m", "a"),
            ("m", "x"), ("x", "y"), ("y", "m"));

        var component = Assert.Single(graph.TwoEdgeConnectedComponents());
        Assert.True(component.SetEquals(["a", "b", "m", "x", "y"]));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_ParallelEdges_AreTwoEdgeConnected()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));

        var components = graph.TwoEdgeConnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.Contains(components, component => component.SetEquals(["a", "b"]));
        Assert.Contains(components, component => component.SetEquals(["c"]));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_SelfLoops_DoNotConnectAnything()
    {
        var graph = Undirected(("a", "a"), ("a", "b"));

        var components = graph.TwoEdgeConnectedComponents();

        Assert.Equal(2, components.Count);
        Assert.All(components, component => Assert.Single(component));
    }

    [Fact]
    public void TwoEdgeConnectedComponents_DirectedGraph_ThrowsArgumentException()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<ArgumentException>(() => graph.TwoEdgeConnectedComponents());
    }

    [Fact]
    public void TwoEdgeConnectedComponents_RandomGraph_MatchesBridgeRemovalComponents()
    {
        var graph = GraphGenerator.ErdosRenyi(60, 0.05, seed: 42);
        var bridges = graph.FindBridges().ToHashSet();

        var expected = new UndirectedMultigraph<int, Edge<int>>();
        foreach (var vertex in graph.Vertices)
        {
            expected.AddVertex(vertex);
        }

        foreach (var edge in graph.Edges.Where(e => !bridges.Contains(e)))
        {
            expected.AddEdge(edge);
        }

        var expectedComponents = expected.ConnectedComponents()
            .Select(component => component.OrderBy(v => v).ToArray())
            .OrderBy(component => component[0])
            .ToList();
        var actualComponents = graph.TwoEdgeConnectedComponents()
            .Select(component => component.OrderBy(v => v).ToArray())
            .OrderBy(component => component[0])
            .ToList();

        Assert.Equal(expectedComponents.Count, actualComponents.Count);
        for (var i = 0; i < expectedComponents.Count; i++)
        {
            Assert.Equal(expectedComponents[i], actualComponents[i]);
        }
    }

    [Fact]
    public void TwoEdgeConnectedComponents_EveryVertexAppearsExactlyOnce()
    {
        var graph = GraphGenerator.ErdosRenyi(60, 0.05, seed: 7);

        var components = graph.TwoEdgeConnectedComponents();

        var all = components.SelectMany(component => component).ToList();
        Assert.Equal(graph.VertexCount, all.Count);
        Assert.Equal(graph.VertexCount, all.Distinct().Count());
    }
}
