using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Imports GraphML documents produced by
/// <see cref="GraphMlExtensions.ToGraphMl{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge})"/>
/// or by other tools. Vertices are strings (GraphML node ids); graphs import
/// into multigraph types, the lossless superset, since GraphML permits
/// parallel edges. Unknown elements and attributes are ignored for forward
/// compatibility; structural problems raise <see cref="FormatException"/>.
/// </summary>
public static class GraphMl
{
    /// <summary>Parses a GraphML document, choosing directedness from its <c>edgedefault</c>.</summary>
    /// <param name="xml">The GraphML document text.</param>
    /// <returns>A directed or undirected multigraph over string vertex ids.</returns>
    /// <exception cref="FormatException">The document is not well-formed GraphML.</exception>
    public static IMutableGraph<string, Edge<string>> Parse(string xml)
    {
        var (graphElement, isDirected) = ReadGraphElement(xml);
        return isDirected
            ? Populate(new DirectedMultigraph<string, Edge<string>>(), graphElement, MakeEdge)
            : Populate(new UndirectedMultigraph<string, Edge<string>>(), graphElement, MakeEdge);

        static Edge<string> MakeEdge(string source, string target, XElement _) => new(source, target);
    }

    /// <summary>Parses a GraphML document that must declare <c>edgedefault="directed"</c>.</summary>
    /// <param name="xml">The GraphML document text.</param>
    /// <returns>The imported directed multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed GraphML or is undirected.</exception>
    public static DirectedMultigraph<string, Edge<string>> ParseDirected(string xml)
        => Populate(
            new DirectedMultigraph<string, Edge<string>>(),
            RequireDirection(xml, directed: true),
            (source, target, _) => new Edge<string>(source, target));

    /// <summary>Parses a GraphML document that must declare <c>edgedefault="undirected"</c>.</summary>
    /// <param name="xml">The GraphML document text.</param>
    /// <returns>The imported undirected multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed GraphML or is directed.</exception>
    public static UndirectedMultigraph<string, Edge<string>> ParseUndirected(string xml)
        => Populate(
            new UndirectedMultigraph<string, Edge<string>>(),
            RequireDirection(xml, directed: false),
            (source, target, _) => new Edge<string>(source, target));

    /// <summary>Parses a directed GraphML document, reading edge weights from <paramref name="weightKey"/> data elements (0 when absent).</summary>
    /// <param name="xml">The GraphML document text.</param>
    /// <param name="weightKey">The data key holding edge weights.</param>
    /// <returns>The imported weighted directed multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed GraphML, is undirected, or holds a non-numeric weight.</exception>
    public static DirectedMultigraph<string, WeightedEdge<string, double>> ParseDirectedWeighted(
        string xml,
        string weightKey = GraphMlExtensions.WeightKeyId)
        => Populate(
            new DirectedMultigraph<string, WeightedEdge<string, double>>(),
            RequireDirection(xml, directed: true),
            (source, target, element) => new WeightedEdge<string, double>(source, target, ReadWeight(element, weightKey)));

    /// <summary>Parses an undirected GraphML document, reading edge weights from <paramref name="weightKey"/> data elements (0 when absent).</summary>
    /// <param name="xml">The GraphML document text.</param>
    /// <param name="weightKey">The data key holding edge weights.</param>
    /// <returns>The imported weighted undirected multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed GraphML, is directed, or holds a non-numeric weight.</exception>
    public static UndirectedMultigraph<string, WeightedEdge<string, double>> ParseUndirectedWeighted(
        string xml,
        string weightKey = GraphMlExtensions.WeightKeyId)
        => Populate(
            new UndirectedMultigraph<string, WeightedEdge<string, double>>(),
            RequireDirection(xml, directed: false),
            (source, target, element) => new WeightedEdge<string, double>(source, target, ReadWeight(element, weightKey)));

    private static (XElement Graph, bool IsDirected) ReadGraphElement(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException exception)
        {
            throw new FormatException("The document is not well-formed XML.", exception);
        }

        // Match by local name so documents with or without the GraphML
        // namespace both import.
        var graphElement = document.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "graph")
            ?? throw new FormatException("The document contains no <graph> element.");

        var edgeDefault = graphElement.Attribute("edgedefault")?.Value
            ?? throw new FormatException("The <graph> element is missing the required edgedefault attribute.");

        return edgeDefault switch
        {
            "directed" => (graphElement, true),
            "undirected" => (graphElement, false),
            _ => throw new FormatException($"Unsupported edgedefault value '{edgeDefault}'."),
        };
    }

    private static XElement RequireDirection(string xml, bool directed)
    {
        var (graphElement, isDirected) = ReadGraphElement(xml);
        if (isDirected != directed)
        {
            throw new FormatException(
                $"The document declares edgedefault=\"{(isDirected ? "directed" : "undirected")}\", "
                + $"but {(directed ? "a directed" : "an undirected")} graph was requested.");
        }

        return graphElement;
    }

    private static TGraph Populate<TGraph, TEdge>(
        TGraph graph,
        XElement graphElement,
        Func<string, string, XElement, TEdge> edgeFactory)
        where TGraph : IMutableGraph<string, TEdge>
        where TEdge : IEdge<string>
    {
        foreach (var element in graphElement.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "node":
                    var id = element.Attribute("id")?.Value
                        ?? throw new FormatException("A <node> element is missing the required id attribute.");
                    graph.AddVertex(id);
                    break;

                case "edge":
                    var source = element.Attribute("source")?.Value
                        ?? throw new FormatException("An <edge> element is missing the required source attribute.");
                    var target = element.Attribute("target")?.Value
                        ?? throw new FormatException("An <edge> element is missing the required target attribute.");
                    graph.AddEdge(edgeFactory(source, target, element));
                    break;

                default:
                    break; // unknown elements are ignored for forward compatibility
            }
        }

        return graph;
    }

    private static double ReadWeight(XElement edgeElement, string weightKey)
    {
        var data = edgeElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "data" && e.Attribute("key")?.Value == weightKey);
        if (data is null)
        {
            return 0.0;
        }

        return double.TryParse(data.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight)
            ? weight
            : throw new FormatException($"Edge weight '{data.Value}' is not a valid number.");
    }
}
