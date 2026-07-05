using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class GraphJsonTests
{
    [Fact]
    public void Export_DirectedGraph_WritesDirectedTrueNodesAndEdges()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var json = graph.ToJson();

        Assert.Contains("\"directed\": true", json);
        Assert.Contains("\"id\": \"a\"", json);
        Assert.Contains("\"source\": \"a\"", json);
        Assert.Contains("\"target\": \"b\"", json);
    }

    [Fact]
    public void Export_UndirectedGraph_WritesDirectedFalse()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Contains("\"directed\": false", graph.ToJson());
    }

    [Fact]
    public void Export_WeightedEdges_IncludeWeights()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5));

        var json = graph.ToJson(new GraphJsonExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
        });

        Assert.Contains("\"weight\": 2.5", json); // invariant culture
    }

    [Fact]
    public void RoundTrip_DirectedWeightedMultigraph_PreservesEverything()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 1.5));
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5)); // parallel
        graph.AddEdge(new WeightedEdge<string, double>("c", "c", 0.5)); // self-loop
        graph.AddVertex("isolated");

        var json = graph.ToJson(new GraphJsonExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
        });
        var restored = GraphJson.ParseDirectedWeighted(json);

        Assert.Equal(graph.VertexCount, restored.VertexCount);
        Assert.Equal(graph.EdgeCount, restored.EdgeCount);
        Assert.True(restored.ContainsVertex("isolated"));
        Assert.Equal(
            graph.Edges.Select(e => (e.Source, e.Target, e.Weight)).OrderBy(t => t).ToList(),
            restored.Edges.Select(e => (e.Source, e.Target, e.Weight)).OrderBy(t => t).ToList());
    }

    [Fact]
    public void RoundTrip_HostileStrings_Survive()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("say \"hi\"", "back\\slash\nnewline"));
        graph.AddVertex("unicode λ→∞");

        var restored = GraphJson.ParseDirected(graph.ToJson());

        Assert.True(restored.ContainsVertex("say \"hi\""));
        Assert.True(restored.ContainsVertex("unicode λ→∞"));
        Assert.Equal(1, restored.EdgeCount);
    }

    [Fact]
    public void Parse_AutoDetectsDirection()
    {
        var directed = new DirectedGraph<string, Edge<string>>();
        directed.AddEdge(new Edge<string>("a", "b"));
        var undirected = new UndirectedGraph<string, Edge<string>>();
        undirected.AddEdge(new Edge<string>("a", "b"));

        Assert.True(GraphJson.Parse(directed.ToJson()).IsDirected);
        Assert.False(GraphJson.Parse(undirected.ToJson()).IsDirected);
    }

    [Fact]
    public void ParseUndirected_SymmetricLookupsWork()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("x", "y"));

        var restored = GraphJson.ParseUndirected(graph.ToJson());

        Assert.True(restored.ContainsEdge("y", "x"));
    }

    [Fact]
    public void ParseDirected_OnUndirectedDocument_Throws()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<FormatException>(() => GraphJson.ParseDirected(graph.ToJson()));
    }

    [Fact]
    public void Parse_MalformedJson_Throws()
    {
        Assert.Throws<FormatException>(() => GraphJson.Parse("{ \"directed\": tru"));
    }

    [Fact]
    public void Parse_MissingDirectedProperty_Throws()
    {
        Assert.Throws<FormatException>(() => GraphJson.Parse("{ \"nodes\": [] }"));
    }

    [Fact]
    public void Parse_MissingNodesAndEdges_YieldsEmptyGraph()
    {
        var graph = GraphJson.Parse("{ \"directed\": true }");

        Assert.Equal(0, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void Parse_EdgeMissingEndpoint_Throws()
    {
        const string json = """{ "directed": true, "edges": [ { "source": "a" } ] }""";

        Assert.Throws<FormatException>(() => GraphJson.Parse(json));
    }

    [Fact]
    public void Parse_NodeMissingId_Throws()
    {
        const string json = """{ "directed": true, "nodes": [ { "name": "a" } ] }""";

        Assert.Throws<FormatException>(() => GraphJson.Parse(json));
    }

    [Fact]
    public void Parse_EdgeReferencingUndeclaredNode_IsLenientlyAccepted()
    {
        const string json = """{ "directed": true, "edges": [ { "source": "a", "target": "b" } ] }""";

        var graph = GraphJson.Parse(json);

        Assert.Equal(2, graph.VertexCount);
        Assert.True(graph.ContainsEdge("a", "b"));
    }

    [Fact]
    public void Parse_UnknownProperties_AreIgnored()
    {
        const string json = """
            {
                "directed": false,
                "metadata": { "creator": "someone" },
                "nodes": [ { "id": "a", "color": "red" } ],
                "edges": [ { "source": "a", "target": "a", "kind": "loop" } ]
            }
            """;

        var graph = GraphJson.Parse(json);

        Assert.Equal(1, graph.VertexCount);
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void ParseWeighted_MissingWeight_DefaultsToZero()
    {
        const string json = """{ "directed": false, "edges": [ { "source": "a", "target": "b" } ] }""";

        Assert.Equal(0.0, GraphJson.ParseUndirectedWeighted(json).Edges.Single().Weight);
    }

    [Fact]
    public void ParseWeighted_NonNumericWeight_Throws()
    {
        const string json = """{ "directed": true, "edges": [ { "source": "a", "target": "b", "weight": "heavy" } ] }""";

        Assert.Throws<FormatException>(() => GraphJson.ParseDirectedWeighted(json));
    }

    [Fact]
    public void CrossFormat_GraphMlToJson_PreservesStructure()
    {
        var original = new DirectedMultigraph<string, Edge<string>>();
        original.AddEdge(new Edge<string>("a", "b"));
        original.AddEdge(new Edge<string>("a", "b"));
        original.AddEdge(new Edge<string>("b", "c"));
        original.AddVertex("isolated");

        var viaGraphMl = GraphMl.ParseDirected(original.ToGraphMl());
        var viaJson = GraphJson.ParseDirected(viaGraphMl.ToJson());

        Assert.Equal(original.VertexCount, viaJson.VertexCount);
        Assert.Equal(original.EdgeCount, viaJson.EdgeCount);
        Assert.Equal(
            original.Edges.Select(e => (e.Source, e.Target)).OrderBy(t => t).ToList(),
            viaJson.Edges.Select(e => (e.Source, e.Target)).OrderBy(t => t).ToList());
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentNullException>(() => ((DirectedGraph<string, Edge<string>>)null!).ToJson());
        Assert.Throws<ArgumentNullException>(() => graph.ToJson(null!));
        Assert.Throws<ArgumentNullException>(() => GraphJson.Parse(null!));
    }
}
