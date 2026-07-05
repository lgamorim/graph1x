using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class CondensationTests
{
    private static DirectedGraph<string, Edge<string>> Directed(params (string, string)[] edges)
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        foreach (var (source, target) in edges)
        {
            graph.AddEdge(new Edge<string>(source, target));
        }

        return graph;
    }

    [Fact]
    public void TwoLinkedCycles_CondenseToTwoComponentsAndOneEdge()
    {
        var graph = Directed(("a", "b"), ("b", "a"), ("b", "c"), ("c", "d"), ("d", "c"));

        var condensation = graph.Condense();

        Assert.Equal(2, condensation.ComponentCount);
        Assert.Equal(2, condensation.Graph.VertexCount);
        Assert.Equal(1, condensation.Graph.EdgeCount);
        Assert.False(condensation.Graph.HasCycle());

        var ab = condensation.ComponentOf("a");
        var cd = condensation.ComponentOf("c");

        Assert.Equal(ab, condensation.ComponentOf("b"));
        Assert.Equal(cd, condensation.ComponentOf("d"));
        Assert.NotEqual(ab, cd);
        Assert.True(condensation.Graph.ContainsEdge(ab, cd));
        Assert.True(condensation.Members(ab).SetEquals(["a", "b"]));
        Assert.True(condensation.Members(cd).SetEquals(["c", "d"]));
    }

    [Fact]
    public void Dag_CondensesToItself_ComponentWise()
    {
        var graph = Directed(("a", "b"), ("a", "c"), ("b", "d"), ("c", "d"));

        var condensation = graph.Condense();

        Assert.Equal(4, condensation.ComponentCount);
        Assert.Equal(4, condensation.Graph.EdgeCount);
        foreach (var edge in graph.Edges)
        {
            Assert.True(condensation.Graph.ContainsEdge(
                condensation.ComponentOf(edge.Source),
                condensation.ComponentOf(edge.Target)));
        }
    }

    [Fact]
    public void SingleCycle_CollapsesToOneVertexWithoutSelfLoop()
    {
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "a"));

        var condensation = graph.Condense();

        Assert.Equal(1, condensation.ComponentCount);
        Assert.Equal(0, condensation.Graph.EdgeCount);
    }

    [Fact]
    public void EdgeIndices_FollowReverseTopologicalEmission()
    {
        // Tarjan emits components in reverse topological order, so every
        // condensation edge points from a higher index to a lower one.
        var graph = Directed(("a", "b"), ("b", "c"), ("c", "b"), ("c", "d"));

        var condensation = graph.Condense();

        Assert.All(condensation.Graph.Edges, edge => Assert.True(edge.Source > edge.Target));
        Assert.NotEmpty(condensation.Graph.TopologicalSort());
    }

    [Fact]
    public void Condensation_IsAlwaysAcyclic_OnSeededRandomDigraphs()
    {
        var random = new Random(20260705);
        for (var round = 0; round < 5; round++)
        {
            var graph = new DirectedGraph<int, Edge<int>>();
            for (var v = 0; v < 12; v++)
            {
                graph.AddVertex(v);
            }

            for (var i = 0; i < 30; i++)
            {
                var a = random.Next(12);
                var b = random.Next(12);
                if (a != b)
                {
                    graph.AddEdge(new Edge<int>(a, b));
                }
            }

            var condensation = graph.Condense();

            Assert.False(condensation.Graph.HasCycle());
            Assert.Equal(graph.StronglyConnectedComponents().Count, condensation.ComponentCount);
        }
    }

    [Fact]
    public void Reachability_IsPreservedBetweenComponents()
    {
        var graph = Directed(("a", "b"), ("b", "a"), ("b", "c"), ("x", "y"));

        var condensation = graph.Condense();
        var closure = condensation.Graph.TransitiveClosure();

        // a reaches c in the original; x never reaches a.
        Assert.True(closure.ContainsEdge(condensation.ComponentOf("a"), condensation.ComponentOf("c")));
        Assert.False(closure.ContainsEdge(condensation.ComponentOf("x"), condensation.ComponentOf("a")));
    }

    [Fact]
    public void Multigraph_ParallelCrossEdges_Dedupe()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var condensation = graph.Condense();

        Assert.Equal(1, condensation.Graph.EdgeCount);
    }

    [Fact]
    public void EmptyGraph_CondensesToEmpty()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        var condensation = graph.Condense();

        Assert.Equal(0, condensation.ComponentCount);
        Assert.Equal(0, condensation.Graph.VertexCount);
    }

    [Fact]
    public void Lookups_AreGuarded()
    {
        var graph = Directed(("a", "b"));
        var condensation = graph.Condense();

        Assert.Throws<ArgumentException>(() => condensation.ComponentOf("ghost"));
        Assert.Throws<ArgumentOutOfRangeException>(() => condensation.Members(99));
        Assert.Throws<ArgumentOutOfRangeException>(() => condensation.Members(-1));
    }

    [Fact]
    public void CustomComparer_FlowsThroughLookups()
    {
        var graph = new DirectedGraph<string, Edge<string>>(StringComparer.OrdinalIgnoreCase);
        graph.AddEdge(new Edge<string>("Lisbon", "Porto"));

        var condensation = graph.Condense();

        Assert.Equal(condensation.ComponentOf("LISBON"), condensation.ComponentOf("lisbon"));
    }
}
