using Graph1x;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Graphs;

public class DirectedAcyclicGraphTests : GraphContractTests
{
    protected override IMutableGraph<string, Edge<string>> CreateGraph()
        => new DirectedAcyclicGraph<string, Edge<string>>();

    protected override IMutableGraph<string, Edge<string>> CreateGraph(IEqualityComparer<string> comparer)
        => new DirectedAcyclicGraph<string, Edge<string>>(comparer);

    private static DirectedAcyclicGraph<string, Edge<string>> CreateDag() => new();

    [Fact]
    public void IsDirected_IsTrue_AndParallelEdgesDisallowed()
    {
        var dag = CreateDag();

        Assert.True(dag.IsDirected);
        Assert.False(dag.AllowsParallelEdges);
    }

    [Fact]
    public void AddEdge_SelfLoop_IsRejected()
    {
        var dag = CreateDag();

        Assert.False(dag.AddEdge(new Edge<string>("a", "a")));
        Assert.Equal(0, dag.EdgeCount);
    }

    [Fact]
    public void AddEdge_DirectBackEdge_IsRejected()
    {
        var dag = CreateDag();
        dag.AddEdge(new Edge<string>("a", "b"));

        Assert.False(dag.AddEdge(new Edge<string>("b", "a")));
        Assert.Equal(1, dag.EdgeCount);
    }

    [Fact]
    public void AddEdge_ClosingLongCycle_IsRejected()
    {
        var dag = CreateDag();
        dag.AddEdge(new Edge<string>("a", "b"));
        dag.AddEdge(new Edge<string>("b", "c"));
        dag.AddEdge(new Edge<string>("c", "d"));

        Assert.False(dag.AddEdge(new Edge<string>("d", "a")));
        Assert.Equal(3, dag.EdgeCount);
        Assert.False(dag.ContainsEdge("d", "a"));
    }

    [Fact]
    public void AddEdge_Diamond_IsAccepted()
    {
        var dag = CreateDag();

        Assert.True(dag.AddEdge(new Edge<string>("a", "b")));
        Assert.True(dag.AddEdge(new Edge<string>("a", "c")));
        Assert.True(dag.AddEdge(new Edge<string>("b", "d")));
        Assert.True(dag.AddEdge(new Edge<string>("c", "d")));
        Assert.Equal(4, dag.EdgeCount);
    }

    [Fact]
    public void AddEdge_DuplicateEndpoints_ReturnsFalse()
    {
        var dag = CreateDag();
        dag.AddEdge(new Edge<string>("a", "b"));

        Assert.False(dag.AddEdge(new Edge<string>("a", "b")));
        Assert.Equal(1, dag.EdgeCount);
    }

    [Fact]
    public void AddEdge_AfterRemovingBlockingEdge_Succeeds()
    {
        var dag = CreateDag();
        dag.AddEdge(new Edge<string>("a", "b"));
        dag.AddEdge(new Edge<string>("b", "c"));

        Assert.False(dag.AddEdge(new Edge<string>("c", "a")));
        Assert.True(dag.RemoveEdge(new Edge<string>("a", "b")));
        Assert.True(dag.AddEdge(new Edge<string>("c", "a")));
    }

    [Fact]
    public void DirectedQueries_AreInherited()
    {
        var dag = CreateDag();
        dag.AddEdge(new Edge<string>("a", "b"));
        dag.AddEdge(new Edge<string>("a", "c"));

        Assert.Equal(2, dag.OutDegree("a"));
        Assert.Equal(1, dag.InDegree("b"));
    }
}
