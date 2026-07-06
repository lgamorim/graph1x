using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class GraphMlAttributeTests
{
    private sealed record City(string Name, int Population, bool Coastal);

    [Fact]
    public void Export_DeclaresTypedKeysAndDataElements()
    {
        var graph = new UndirectedGraph<City, Edge<City>>();
        var lisbon = new City("Lisbon", 545_000, Coastal: true);
        var porto = new City("Porto", 231_000, Coastal: true);
        graph.AddEdge(new Edge<City>(lisbon, porto));

        var xml = graph.ToGraphMl(new GraphMlExportOptions<City, Edge<City>>
        {
            VertexId = city => city.Name,
            VertexAttributes =
            [
                GraphAttribute<City>.Int("population", city => city.Population),
                GraphAttribute<City>.Bool("coastal", city => city.Coastal),
            ],
            EdgeAttributes = [GraphAttribute<Edge<City>>.String("kind", _ => "road")],
        });

        Assert.Contains("<key id=\"population\" for=\"node\" attr.name=\"population\" attr.type=\"int\"", xml);
        Assert.Contains("<key id=\"coastal\" for=\"node\" attr.name=\"coastal\" attr.type=\"boolean\"", xml);
        Assert.Contains("<key id=\"kind\" for=\"edge\" attr.name=\"kind\" attr.type=\"string\"", xml);
        Assert.Contains("<data key=\"population\">545000</data>", xml);
        Assert.Contains("<data key=\"coastal\">true</data>", xml);
        Assert.Contains("<data key=\"kind\">road</data>", xml);
    }

    [Fact]
    public void RoundTrip_PreservesTypedAttributeValues()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5));

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
            VertexAttributes =
            [
                GraphAttribute<string>.String("label", vertex => vertex.ToUpperInvariant()),
                GraphAttribute<string>.Int("rank", vertex => vertex.Length),
                GraphAttribute<string>.Long("big", _ => 5_000_000_000L),
                GraphAttribute<string>.Double("score", _ => 0.25),
                GraphAttribute<string>.Float("ratio", _ => 0.5f),
                GraphAttribute<string>.Bool("active", _ => true),
            ],
        });

        var document = GraphMl.ParseDocument(xml);

        Assert.True(document.Graph.IsDirected);
        Assert.Equal(2, document.Graph.VertexCount);
        Assert.Equal(1, document.Graph.EdgeCount);
        Assert.Equal("A", document.VertexData["a"]["label"]);
        Assert.Equal(1, document.VertexData["a"]["rank"]);
        Assert.Equal(5_000_000_000L, document.VertexData["b"]["big"]);
        Assert.Equal(0.25, document.VertexData["b"]["score"]);
        Assert.Equal(0.5f, document.VertexData["b"]["ratio"]);
        Assert.Equal(true, document.VertexData["a"]["active"]);
        Assert.Equal(2.5, document.EdgeData[0]["weight"]);
    }

    [Fact]
    public void Export_NullStringValue_OmitsTheDataElement()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");
        graph.AddVertex("b");

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, Edge<string>>
        {
            VertexAttributes =
            [
                GraphAttribute<string>.String("nickname", vertex => vertex == "a" ? "alpha" : null),
            ],
        });
        var document = GraphMl.ParseDocument(xml);

        Assert.Equal("alpha", document.VertexData["a"]["nickname"]);
        Assert.False(document.VertexData["b"].ContainsKey("nickname"));
    }

    [Fact]
    public void RoundTrip_EscapesXmlSignificantCharacters()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, Edge<string>>
        {
            VertexAttributes = [GraphAttribute<string>.String("note", _ => "<tags> & \"quotes\"")],
        });
        var document = GraphMl.ParseDocument(xml);

        Assert.Equal("<tags> & \"quotes\"", document.VertexData["a"]["note"]);
    }

    [Fact]
    public void ParseDocument_EdgeDataFollowsInsertionOrder()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 1));
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 2)); // parallel

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, WeightedEdge<string, int>>
        {
            EdgeWeight = edge => edge.Weight,
        });
        var document = GraphMl.ParseDocument(xml);

        Assert.Equal(2, document.EdgeData.Count);
        Assert.Equal(1.0, document.EdgeData[0]["weight"]);
        Assert.Equal(2.0, document.EdgeData[1]["weight"]);
    }

    [Fact]
    public void ParseDocument_WithoutAttributes_YieldsEmptyData()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var document = GraphMl.ParseDocument(graph.ToGraphMl());

        Assert.False(document.Graph.IsDirected);
        Assert.Empty(document.VertexData["a"]);
        Assert.Empty(document.EdgeData[0]);
    }

    [Fact]
    public void ParseDocument_ResolvesKeyIdsToAttributeNames()
    {
        // The NetworkX/yEd shape: opaque key ids carrying attr.name.
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns">
              <key id="d0" for="node" attr.name="color" attr.type="string"/>
              <key id="d1" for="edge" attr.name="capacity" attr.type="int"/>
              <graph id="G" edgedefault="undirected">
                <node id="a"><data key="d0">red</data></node>
                <node id="b"/>
                <edge source="a" target="b"><data key="d1">17</data></edge>
              </graph>
            </graphml>
            """;

        var document = GraphMl.ParseDocument(xml);

        Assert.Equal("red", document.VertexData["a"]["color"]);
        Assert.Empty(document.VertexData["b"]);
        Assert.Equal(17, document.EdgeData[0]["capacity"]);
    }

    [Fact]
    public void ParseDocument_UndeclaredDataKeys_ImportAsStrings()
    {
        const string xml = """
            <graphml>
              <graph edgedefault="directed">
                <node id="a"><data key="mystery">42</data></node>
              </graph>
            </graphml>
            """;

        var document = GraphMl.ParseDocument(xml);

        Assert.Equal("42", document.VertexData["a"]["mystery"]);
    }

    [Fact]
    public void ParseDocument_TypeMismatch_Throws()
    {
        const string xml = """
            <graphml>
              <key id="rank" for="node" attr.name="rank" attr.type="int"/>
              <graph edgedefault="directed">
                <node id="a"><data key="rank">not-a-number</data></node>
              </graph>
            </graphml>
            """;

        Assert.Throws<FormatException>(() => GraphMl.ParseDocument(xml));
    }

    [Fact]
    public void ParseDocument_MalformedDocuments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => GraphMl.ParseDocument(null!));
        Assert.Throws<FormatException>(() => GraphMl.ParseDocument("not xml"));
        Assert.Throws<FormatException>(() => GraphMl.ParseDocument("<graphml></graphml>"));
    }
}
