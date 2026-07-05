using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class GraphMlTests
{
    [Fact]
    public void Export_DirectedGraph_DeclaresDirectedEdgeDefault()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var xml = graph.ToGraphMl();

        Assert.Contains("edgedefault=\"directed\"", xml);
        Assert.Contains("<node id=\"a\"", xml);
        Assert.Contains("source=\"a\"", xml);
        Assert.Contains("target=\"b\"", xml);
    }

    [Fact]
    public void Export_UndirectedGraph_DeclaresUndirectedEdgeDefault()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Contains("edgedefault=\"undirected\"", graph.ToGraphMl());
    }

    [Fact]
    public void Export_WeightedEdges_EmitKeyAndDataElements()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5));

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
        });

        Assert.Contains("attr.name=\"weight\"", xml);
        Assert.Contains(">2.5<", xml); // invariant culture, no comma
    }

    [Fact]
    public void Export_EscapesXmlHostileNames()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("<a&b>", "\"quoted\""));

        var xml = graph.ToGraphMl();

        Assert.Contains("&lt;a&amp;b&gt;", xml);
        Assert.DoesNotContain("<a&b>", xml);
    }

    [Fact]
    public void RoundTrip_DirectedWeightedMultigraph_PreservesEverything()
    {
        var graph = new DirectedMultigraph<string, WeightedEdge<string, double>>();
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 1.5));
        graph.AddEdge(new WeightedEdge<string, double>("a", "b", 2.5)); // parallel
        graph.AddEdge(new WeightedEdge<string, double>("c", "c", 0.5)); // self-loop
        graph.AddVertex("isolated");

        var xml = graph.ToGraphMl(new GraphMlExportOptions<string, WeightedEdge<string, double>>
        {
            EdgeWeight = edge => edge.Weight,
        });
        var restored = GraphMl.ParseDirectedWeighted(xml);

        Assert.Equal(graph.VertexCount, restored.VertexCount);
        Assert.Equal(graph.EdgeCount, restored.EdgeCount);
        Assert.True(restored.ContainsVertex("isolated"));
        Assert.Equal(
            graph.Edges.Select(e => (e.Source, e.Target, e.Weight)).OrderBy(t => t).ToList(),
            restored.Edges.Select(e => (e.Source, e.Target, e.Weight)).OrderBy(t => t).ToList());
    }

    [Fact]
    public void RoundTrip_UndirectedGraph_PreservesEdges()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("x", "y"));
        graph.AddEdge(new Edge<string>("y", "z"));

        var restored = GraphMl.ParseUndirected(graph.ToGraphMl());

        Assert.False(restored.IsDirected);
        Assert.Equal(2, restored.EdgeCount);
        Assert.True(restored.ContainsEdge("z", "y"));
    }

    [Fact]
    public void RoundTrip_HostileVertexNames_Survive()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("<a&b>", "line\nbreak"));

        var restored = GraphMl.ParseDirected(graph.ToGraphMl());

        Assert.True(restored.ContainsVertex("<a&b>"));
    }

    [Fact]
    public void Parse_AutoDetectsDirection()
    {
        var directed = new DirectedGraph<string, Edge<string>>();
        directed.AddEdge(new Edge<string>("a", "b"));
        var undirected = new UndirectedGraph<string, Edge<string>>();
        undirected.AddEdge(new Edge<string>("a", "b"));

        Assert.True(GraphMl.Parse(directed.ToGraphMl()).IsDirected);
        Assert.False(GraphMl.Parse(undirected.ToGraphMl()).IsDirected);
    }

    [Fact]
    public void ParseDirected_OnUndirectedDocument_Throws()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Throws<FormatException>(() => GraphMl.ParseDirected(graph.ToGraphMl()));
    }

    [Fact]
    public void Parse_MalformedXml_Throws()
    {
        Assert.Throws<FormatException>(() => GraphMl.Parse("<graphml><graph edgedefault="));
    }

    [Fact]
    public void Parse_MissingGraphElement_Throws()
    {
        Assert.Throws<FormatException>(() => GraphMl.Parse("<graphml></graphml>"));
    }

    [Fact]
    public void Parse_EdgeMissingEndpoint_Throws()
    {
        const string xml = """
            <graphml><graph edgedefault="directed">
                <node id="a" />
                <edge source="a" />
            </graph></graphml>
            """;

        Assert.Throws<FormatException>(() => GraphMl.Parse(xml));
    }

    [Fact]
    public void Parse_MissingEdgeDefault_Throws()
    {
        Assert.Throws<FormatException>(() => GraphMl.Parse("<graphml><graph></graph></graphml>"));
    }

    [Fact]
    public void Parse_IgnoresUnknownElementsAndAttributes()
    {
        const string xml = """
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns">
                <key id="color" for="node" attr.name="color" attr.type="string" />
                <graph id="G" edgedefault="directed" custom="ignored">
                    <node id="a"><data key="color">red</data></node>
                    <node id="b" />
                    <edge source="a" target="b" id="e0" />
                    <unknown-element />
                </graph>
            </graphml>
            """;

        var graph = GraphMl.Parse(xml);

        Assert.Equal(2, graph.VertexCount);
        Assert.Equal(1, graph.EdgeCount);
        Assert.True(graph.ContainsEdge("a", "b"));
    }

    [Fact]
    public void ParseWeighted_MissingWeightData_DefaultsToZero()
    {
        const string xml = """
            <graphml><graph edgedefault="undirected">
                <node id="a" />
                <node id="b" />
                <edge source="a" target="b" />
            </graph></graphml>
            """;

        var graph = GraphMl.ParseUndirectedWeighted(xml);

        Assert.Equal(0.0, graph.Edges.Single().Weight);
    }

    [Fact]
    public void ParseWeighted_MalformedWeight_Throws()
    {
        const string xml = """
            <graphml>
                <key id="weight" for="edge" attr.name="weight" attr.type="double" />
                <graph edgedefault="directed">
                    <node id="a" /><node id="b" />
                    <edge source="a" target="b"><data key="weight">not-a-number</data></edge>
                </graph>
            </graphml>
            """;

        Assert.Throws<FormatException>(() => GraphMl.ParseDirectedWeighted(xml));
    }

    [Fact]
    public void Parse_EdgeReferencingUndeclaredNode_IsLenientlyAccepted()
    {
        const string xml = """
            <graphml><graph edgedefault="directed">
                <edge source="a" target="b" />
            </graph></graphml>
            """;

        var graph = GraphMl.Parse(xml);

        Assert.Equal(2, graph.VertexCount);
        Assert.True(graph.ContainsEdge("a", "b"));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentNullException>(() => ((DirectedGraph<string, Edge<string>>)null!).ToGraphMl());
        Assert.Throws<ArgumentNullException>(() => graph.ToGraphMl(null!));
        Assert.Throws<ArgumentNullException>(() => GraphMl.Parse(null!));
    }
}
