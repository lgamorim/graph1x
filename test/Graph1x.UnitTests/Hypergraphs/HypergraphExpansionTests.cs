using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Hypergraphs;

namespace Graph1x.UnitTests.Hypergraphs;

public class HypergraphExpansionTests
{
    [Fact]
    public void CliqueExpansion_SingleHyperedge_BecomesAClique()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b", "c");

        var clique = hypergraph.ToCliqueExpansion();

        Assert.Equal(3, clique.VertexCount);
        Assert.Equal(3, clique.EdgeCount); // triangle
        Assert.True(clique.ContainsEdge("a", "b"));
        Assert.True(clique.ContainsEdge("b", "c"));
        Assert.True(clique.ContainsEdge("a", "c"));
    }

    [Fact]
    public void CliqueExpansion_OverlappingHyperedges_DedupSharedPairs()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");
        hypergraph.AddHyperedge("a", "b", "c");

        var clique = hypergraph.ToCliqueExpansion();

        Assert.Equal(3, clique.EdgeCount); // a-b once, plus a-c and b-c
    }

    [Fact]
    public void CliqueExpansion_SingletonHyperedge_AddsNoEdges()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a");

        var clique = hypergraph.ToCliqueExpansion();

        Assert.True(clique.ContainsVertex("a"));
        Assert.Equal(0, clique.EdgeCount);
    }

    [Fact]
    public void CliqueExpansion_PreservesIsolatedVerticesAndEmptiness()
    {
        var hypergraph = new Hypergraph<string>();

        Assert.Equal(0, hypergraph.ToCliqueExpansion().VertexCount);

        hypergraph.AddVertex("lonely");

        Assert.True(hypergraph.ToCliqueExpansion().ContainsVertex("lonely"));
    }

    [Fact]
    public void CliqueExpansion_ComponentsMatchTheHypergraph()
    {
        var random = new Random(20260703);
        var hypergraph = new Hypergraph<int>();
        for (var v = 0; v < 15; v++)
        {
            hypergraph.AddVertex(v);
        }

        for (var i = 0; i < 8; i++)
        {
            var size = random.Next(1, 5);
            hypergraph.AddHyperedge(Enumerable.Range(0, size).Select(_ => random.Next(15)).ToArray());
        }

        Assert.Equal(
            hypergraph.ConnectedComponents().Count,
            hypergraph.ToCliqueExpansion().ConnectedComponents().Count);
    }

    [Fact]
    public void CliqueExpansion_PreservesComparer()
    {
        var hypergraph = new Hypergraph<string>(StringComparer.OrdinalIgnoreCase);
        hypergraph.AddHyperedge("Ana", "BRUNO");

        Assert.True(hypergraph.ToCliqueExpansion().ContainsEdge("ana", "bruno"));
    }

    [Fact]
    public void IncidenceGraph_HasVertexAndHyperedgeNodes()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b", "c");
        hypergraph.AddHyperedge("c", "d");

        var incidence = hypergraph.ToBipartiteIncidenceGraph();

        Assert.Equal(4 + 2, incidence.VertexCount);          // vertices + hyperedge nodes
        Assert.Equal(3 + 2, incidence.EdgeCount);            // total membership count
        Assert.True(incidence.IsBipartite());
        Assert.True(incidence.ContainsEdge(
            IncidenceVertex.ForVertex("c"),
            IncidenceVertex.ForHyperedge<string>(1)));
    }

    [Fact]
    public void IncidenceGraph_CoMembers_AreTwoHopsApart()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b", "c");

        var incidence = hypergraph.ToBipartiteIncidenceGraph();
        var hops = incidence.ShortestPath(
            IncidenceVertex.ForVertex("a"),
            IncidenceVertex.ForVertex("c"),
            _ => 1);

        Assert.Equal(2, hops.Distance); // a -> hyperedge node -> c
    }

    [Fact]
    public void IncidenceGraph_ComponentsMatchTheHypergraph()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");
        hypergraph.AddHyperedge("b", "c");
        hypergraph.AddHyperedge("x", "y");
        hypergraph.AddVertex("lonely");

        Assert.Equal(
            hypergraph.ConnectedComponents().Count,
            hypergraph.ToBipartiteIncidenceGraph().ConnectedComponents().Count);
    }

    [Fact]
    public void IncidenceGraph_DuplicateVertexSets_GetDistinctNodes()
    {
        var hypergraph = new Hypergraph<string>();
        hypergraph.AddHyperedge("a", "b");
        hypergraph.AddHyperedge("a", "b");

        var incidence = hypergraph.ToBipartiteIncidenceGraph();

        Assert.Equal(2 + 2, incidence.VertexCount);
        Assert.Equal(4, incidence.EdgeCount);
        Assert.Equal(2, incidence.Degree(IncidenceVertex.ForVertex("a")));
    }

    [Fact]
    public void IncidenceGraph_RespectsVertexComparer()
    {
        var hypergraph = new Hypergraph<string>(StringComparer.OrdinalIgnoreCase);
        hypergraph.AddHyperedge("Ana", "Bruno");

        var incidence = hypergraph.ToBipartiteIncidenceGraph();

        Assert.True(incidence.ContainsVertex(IncidenceVertex.ForVertex("ANA")));
    }

    [Fact]
    public void IncidenceVertex_AccessorsAreGuarded()
    {
        var vertexNode = IncidenceVertex.ForVertex("a");
        var hyperedgeNode = IncidenceVertex.ForHyperedge<string>(3);

        Assert.False(vertexNode.IsHyperedge);
        Assert.Equal("a", vertexNode.Vertex);
        Assert.Throws<InvalidOperationException>(() => vertexNode.HyperedgeIndex);

        Assert.True(hyperedgeNode.IsHyperedge);
        Assert.Equal(3, hyperedgeNode.HyperedgeIndex);
        Assert.Throws<InvalidOperationException>(() => hyperedgeNode.Vertex);

        Assert.Equal(IncidenceVertex.ForVertex("a"), vertexNode);
        Assert.NotEqual(IncidenceVertex.ForHyperedge<string>(2), hyperedgeNode);
        Assert.NotEqual(vertexNode, hyperedgeNode);
    }

    [Fact]
    public void NullHypergraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((Hypergraph<string>)null!).ToCliqueExpansion());
        Assert.Throws<ArgumentNullException>(() => ((Hypergraph<string>)null!).ToBipartiteIncidenceGraph());
    }
}
