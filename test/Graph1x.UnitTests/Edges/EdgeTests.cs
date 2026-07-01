using Graph1x.Edges;

namespace Graph1x.UnitTests.Edges;

public class EdgeTests
{
    [Fact]
    public void Constructor_SetsSourceAndTarget()
    {
        var edge = new Edge<string>("a", "b");

        Assert.Equal("a", edge.Source);
        Assert.Equal("b", edge.Target);
    }

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Edge<string>(null!, "b"));
    }

    [Fact]
    public void Constructor_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Edge<string>("a", null!));
    }

    [Fact]
    public void Edges_WithSameEndpoints_AreEqual()
    {
        var first = new Edge<string>("a", "b");
        var second = new Edge<string>("a", "b");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
    }

    [Fact]
    public void Edges_WithSwappedEndpoints_AreNotEqual()
    {
        // Edge values are ordered pairs; undirected (a-b == b-a) semantics
        // live at the graph level, not the edge level.
        var forward = new Edge<string>("a", "b");
        var backward = new Edge<string>("b", "a");

        Assert.NotEqual(forward, backward);
    }

    [Fact]
    public void Edges_WithDifferentEndpoints_AreNotEqual()
    {
        var first = new Edge<string>("a", "b");
        var second = new Edge<string>("a", "c");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SelfLoop_IsAllowedAtEdgeLevel()
    {
        var loop = new Edge<int>(7, 7);

        Assert.Equal(loop.Source, loop.Target);
    }

    [Fact]
    public void Deconstruct_ReturnsSourceAndTarget()
    {
        var (source, target) = new Edge<string>("a", "b");

        Assert.Equal("a", source);
        Assert.Equal("b", target);
    }

    [Fact]
    public void Edge_ImplementsIEdge()
    {
        IEdge<string> edge = new Edge<string>("a", "b");

        Assert.Equal("a", edge.Source);
        Assert.Equal("b", edge.Target);
    }
}
