using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class GraphJsonAttributeTests
{
    [Fact]
    public void Export_WritesTypedJsonValues()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var json = graph.ToJson(new GraphJsonExportOptions<string, Edge<string>>
        {
            VertexAttributes =
            [
                GraphAttribute<string>.String("label", vertex => vertex.ToUpperInvariant()),
                GraphAttribute<string>.Int("rank", vertex => vertex.Length),
                GraphAttribute<string>.Bool("active", _ => true),
            ],
            EdgeAttributes = [GraphAttribute<Edge<string>>.Double("length", _ => 1.5)],
        });

        Assert.Contains("\"label\": \"A\"", json);
        Assert.Contains("\"rank\": 1", json);
        Assert.Contains("\"active\": true", json);
        Assert.Contains("\"length\": 1.5", json);
    }

    [Fact]
    public void RoundTrip_PreservesAttributeValues()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5));

        var json = graph.ToJson(new GraphJsonExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
            VertexAttributes =
            [
                GraphAttribute<string>.String("label", vertex => vertex.ToUpperInvariant()),
                GraphAttribute<string>.Int("rank", vertex => vertex.Length),
                GraphAttribute<string>.Bool("active", vertex => vertex == "a"),
            ],
        });

        var document = GraphJson.ParseDocument(json);

        Assert.False(document.Graph.IsDirected);
        Assert.Equal(2, document.Graph.VertexCount);
        Assert.Equal("A", document.VertexData["a"]["label"]);
        Assert.Equal(1.0, document.VertexData["a"]["rank"]); // JSON numbers parse as double
        Assert.Equal(true, document.VertexData["a"]["active"]);
        Assert.Equal(false, document.VertexData["b"]["active"]);
        Assert.Equal(2.5, document.EdgeData[0]["weight"]);
    }

    [Fact]
    public void Export_NullStringValue_OmitsTheProperty()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        var json = graph.ToJson(new GraphJsonExportOptions<string, Edge<string>>
        {
            VertexAttributes =
            [
                GraphAttribute<string>.String("nickname", vertex => vertex == "a" ? "alpha" : null),
            ],
        });
        var document = GraphJson.ParseDocument(json);

        Assert.Equal("alpha", document.VertexData["a"]["nickname"]);
        Assert.False(document.VertexData["b"].ContainsKey("nickname"));
    }

    [Fact]
    public void RoundTrip_EscapesJsonSignificantCharacters()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        var json = graph.ToJson(new GraphJsonExportOptions<string, Edge<string>>
        {
            VertexAttributes = [GraphAttribute<string>.String("note", _ => "line\nbreak \"quoted\" \\slash")],
        });
        var document = GraphJson.ParseDocument(json);

        Assert.Equal("line\nbreak \"quoted\" \\slash", document.VertexData["a"]["note"]);
    }

    [Fact]
    public void ParseDocument_EdgeDataFollowsInsertionOrder()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2)); // parallel

        var json = graph.ToJson(new GraphJsonExportOptions<string, WeightedEdge<string, int>>
        {
            EdgeWeight = edge => edge.Weight,
        });
        var document = GraphJson.ParseDocument(json);

        Assert.Equal(2, document.EdgeData.Count);
        Assert.Equal(1.0, document.EdgeData[0]["weight"]);
        Assert.Equal(2.0, document.EdgeData[1]["weight"]);
    }

    [Fact]
    public void ParseDocument_EdgesInterleavedAcrossSources_KeepsEdgeDataInDocumentOrder()
    {
        // Foreign documents may order edges freely; Graph1x's own exports
        // group them by source vertex. EdgeData stays in document order, so
        // it must be correlated by document position, not by zipping with
        // the graph's edge enumeration.
        const string json = """
            {
              "directed": true,
              "nodes": [{ "id": "a" }, { "id": "b" }, { "id": "c" }],
              "edges": [
                { "source": "b", "target": "c", "w": 1 },
                { "source": "a", "target": "b", "w": 2 }
              ]
            }
            """;

        var document = GraphJson.ParseDocument(json);

        Assert.Equal(1.0, document.EdgeData[0]["w"]);
        Assert.Equal(2.0, document.EdgeData[1]["w"]);

        // The graph itself enumerates edges grouped by source vertex.
        Assert.Equal(new Edge<string>("a", "b"), document.Graph.Edges.First());
    }

    [Fact]
    public void ParseDocument_ReservedPropertiesAreNotAttributes()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var document = GraphJson.ParseDocument(graph.ToJson());

        Assert.Empty(document.VertexData["a"]); // "id" is structural
        Assert.Empty(document.EdgeData[0]);     // "source"/"target" are structural
    }

    [Fact]
    public void ParseDocument_NonScalarValues_AreIgnored()
    {
        // The NetworkX ecosystem sometimes attaches lists; scalars only here.
        const string json = """
            {
              "directed": false,
              "nodes": [{ "id": "a", "tags": ["x", "y"], "meta": { "k": 1 }, "gone": null, "size": 3 }],
              "edges": []
            }
            """;

        var document = GraphJson.ParseDocument(json);

        Assert.Single(document.VertexData["a"]);
        Assert.Equal(3.0, document.VertexData["a"]["size"]);
    }

    [Fact]
    public void ParseDocument_MalformedDocuments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => GraphJson.ParseDocument(null!));
        Assert.Throws<FormatException>(() => GraphJson.ParseDocument("not json"));
        Assert.Throws<FormatException>(() => GraphJson.ParseDocument("{ \"nodes\": [] }"));
    }
}
