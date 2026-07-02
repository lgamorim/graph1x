using Graph1x.Hypergraphs;

namespace Graph1x.UnitTests.Hypergraphs;

public class HypergraphTests
{
    [Fact]
    public void NewHypergraph_IsEmpty()
    {
        var hypergraph = new Hypergraph<string>();

        Assert.Equal(0, hypergraph.VertexCount);
        Assert.Equal(0, hypergraph.HyperedgeCount);
        Assert.Empty(hypergraph.Vertices);
        Assert.Empty(hypergraph.Hyperedges);
    }

    [Fact]
    public void AddVertex_NewVertex_ReturnsTrue()
    {
        var hypergraph = new Hypergraph<string>();

        Assert.True(hypergraph.AddVertex("a"));
        Assert.True(hypergraph.ContainsVertex("a"));
        Assert.False(hypergraph.AddVertex("a"));
    }

    [Fact]
    public void AddVertex_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Hypergraph<string>().AddVertex(null!));
    }

    [Fact]
    public void AddHyperedge_AutoAddsVertices_AndReturnsHandle()
    {
        var hypergraph = new Hypergraph<string>();

        var meeting = hypergraph.AddHyperedge("ana", "bruno", "carla");

        Assert.Equal(3, hypergraph.VertexCount);
        Assert.Equal(1, hypergraph.HyperedgeCount);
        Assert.Equal(3, meeting.Size);
        Assert.True(meeting.Contains("bruno"));
        Assert.Contains(meeting, hypergraph.Hyperedges);
    }

    [Fact]
    public void AddHyperedge_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Hypergraph<string>().AddHyperedge());
    }

    [Fact]
    public void AddHyperedge_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Hypergraph<string>().AddHyperedge((IEnumerable<string>)null!));
    }

    [Fact]
    public void AddHyperedge_NullVertex_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Hypergraph<string>().AddHyperedge("a", null!));
    }

    [Fact]
    public void AddHyperedge_RepeatedVertices_AreDeduplicated()
    {
        var hypergraph = new Hypergraph<string>();

        var edge = hypergraph.AddHyperedge("a", "a", "b");

        Assert.Equal(2, edge.Size);
    }

    [Fact]
    public void AddHyperedge_Singleton_IsAllowed()
    {
        var hypergraph = new Hypergraph<string>();

        var loop = hypergraph.AddHyperedge("a");

        Assert.Equal(1, loop.Size);
        Assert.Equal(1, hypergraph.Degree("a"));
    }

    [Fact]
    public void AddHyperedge_DuplicateVertexSets_AreDistinctInstances()
    {
        var hypergraph = new Hypergraph<string>();
        var first = hypergraph.AddHyperedge("a", "b");
        var second = hypergraph.AddHyperedge("a", "b");

        Assert.Equal(2, hypergraph.HyperedgeCount);
        Assert.NotSame(first, second);

        Assert.True(hypergraph.RemoveHyperedge(first));
        Assert.Equal(1, hypergraph.HyperedgeCount);
        Assert.Contains(second, hypergraph.IncidentHyperedges("a"));
    }

    [Fact]
    public void IncidentHyperedges_And_Degree_CountMembership()
    {
        var hypergraph = new Hypergraph<string>();
        var one = hypergraph.AddHyperedge("a", "b");
        var two = hypergraph.AddHyperedge("a", "c", "d");

        Assert.Equal(2, hypergraph.Degree("a"));
        Assert.Equal(1, hypergraph.Degree("d"));
        Assert.Equal([one, two], hypergraph.IncidentHyperedges("a").ToList());
    }

    [Fact]
    public void IncidentHyperedges_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Hypergraph<string>().IncidentHyperedges("ghost").ToList());
    }

    [Fact]
    public void Degree_MissingVertex_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Hypergraph<string>().Degree("ghost"));
    }

    [Fact]
    public void RemoveHyperedge_UpdatesIncidence()
    {
        var hypergraph = new Hypergraph<string>();
        var edge = hypergraph.AddHyperedge("a", "b");

        Assert.True(hypergraph.RemoveHyperedge(edge));
        Assert.Equal(0, hypergraph.HyperedgeCount);
        Assert.Equal(0, hypergraph.Degree("a"));
        Assert.True(hypergraph.ContainsVertex("a")); // vertices stay
        Assert.False(hypergraph.RemoveHyperedge(edge));
    }

    [Fact]
    public void RemoveVertex_CascadesIncidentHyperedges()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b", "c");
        var survivor = hypergraph.AddHyperedge("c", "d");

        Assert.True(hypergraph.RemoveVertex("a"));
        Assert.False(hypergraph.ContainsVertex("a"));
        Assert.Equal(1, hypergraph.HyperedgeCount);
        Assert.Equal([survivor], hypergraph.IncidentHyperedges("c").ToList());
        Assert.Equal(0, hypergraph.Degree("b"));
    }

    [Fact]
    public void RemoveVertex_Missing_ReturnsFalse()
    {
        Assert.False(new Hypergraph<string>().RemoveVertex("ghost"));
    }

    [Fact]
    public void ConnectedComponents_LinkThroughSharedVertices()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");
        hypergraph.AddHyperedge("b", "c", "d");
        hypergraph.AddHyperedge("x", "y");
        hypergraph.AddVertex("lonely");

        var components = hypergraph.ConnectedComponents();

        Assert.Equal(3, components.Count);
        Assert.Contains(components, c => c.SetEquals(["a", "b", "c", "d"]));
        Assert.Contains(components, c => c.SetEquals(["x", "y"]));
        Assert.Contains(components, c => c.SetEquals(["lonely"]));
    }

    [Fact]
    public void AreConnected_IsTransitiveAcrossHyperedges()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");
        hypergraph.AddHyperedge("b", "c");
        hypergraph.AddVertex("island");

        Assert.True(hypergraph.AreConnected("a", "c"));
        Assert.True(hypergraph.AreConnected("a", "a"));
        Assert.False(hypergraph.AreConnected("a", "island"));
    }

    [Fact]
    public void AreConnected_MissingVertex_Throws()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddVertex("a");

        Assert.Throws<ArgumentException>(() => hypergraph.AreConnected("a", "ghost"));
    }

    [Fact]
    public void CustomComparer_IdentifiesVerticesThroughIt()
    {
        var hypergraph = new Hypergraph<string>(StringComparer.OrdinalIgnoreCase);
        var edge = hypergraph.AddHyperedge("Ana", "BRUNO");

        Assert.True(hypergraph.ContainsVertex("ana"));
        Assert.True(edge.Contains("bruno"));
        Assert.Equal(1, hypergraph.Degree("ANA"));
    }

    [Fact]
    public void Clear_EmptiesEverything()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");

        hypergraph.Clear();

        Assert.Equal(0, hypergraph.VertexCount);
        Assert.Equal(0, hypergraph.HyperedgeCount);
    }
}
