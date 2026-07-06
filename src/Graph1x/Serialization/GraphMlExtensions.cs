using System.Globalization;
using System.Xml.Linq;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Exports graphs to GraphML (http://graphml.graphdrawing.org). Vertices are
/// declared first (isolated ones included) and edges follow in the graph's
/// enumeration order (grouped by source vertex); XML escaping is handled by
/// the writer, so arbitrary vertex strings are safe. Weights are written with
/// the invariant culture.
/// </summary>
public static class GraphMlExtensions
{
    internal static readonly XNamespace Namespace = "http://graphml.graphdrawing.org/xmlns";
    internal const string WeightKeyId = "weight";

    /// <summary>Renders the graph in GraphML with default options.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <returns>The GraphML document text.</returns>
    public static string ToGraphMl<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ToGraphMl(new GraphMlExportOptions<TVertex, TEdge>());

    /// <summary>Renders the graph in GraphML.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <param name="options">Rendering options.</param>
    /// <returns>The GraphML document text; <c>edgedefault</c> follows <see cref="IReadOnlyGraph{TVertex, TEdge}.IsDirected"/>.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static string ToGraphMl<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        GraphMlExportOptions<TVertex, TEdge> options)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var vertexId = options.VertexId ?? (vertex => vertex.ToString() ?? string.Empty);

        var graphElement = new XElement(
            Namespace + "graph",
            new XAttribute("id", options.GraphId),
            new XAttribute("edgedefault", graph.IsDirected ? "directed" : "undirected"));

        foreach (var vertex in graph.Vertices)
        {
            var nodeElement = new XElement(Namespace + "node", new XAttribute("id", vertexId(vertex)));
            AddDataElements(nodeElement, options.VertexAttributes, vertex);
            graphElement.Add(nodeElement);
        }

        foreach (var edge in graph.Edges)
        {
            var edgeElement = new XElement(
                Namespace + "edge",
                new XAttribute("source", vertexId(edge.Source)),
                new XAttribute("target", vertexId(edge.Target)));

            if (options.EdgeWeight is not null)
            {
                edgeElement.Add(new XElement(
                    Namespace + "data",
                    new XAttribute("key", WeightKeyId),
                    options.EdgeWeight(edge).ToString(CultureInfo.InvariantCulture)));
            }

            AddDataElements(edgeElement, options.EdgeAttributes, edge);
            graphElement.Add(edgeElement);
        }

        var root = new XElement(Namespace + "graphml");
        if (options.EdgeWeight is not null)
        {
            root.Add(new XElement(
                Namespace + "key",
                new XAttribute("id", WeightKeyId),
                new XAttribute("for", "edge"),
                new XAttribute("attr.name", "weight"),
                new XAttribute("attr.type", "double")));
        }

        AddKeyDeclarations(root, options.VertexAttributes, "node");
        AddKeyDeclarations(root, options.EdgeAttributes, "edge");

        root.Add(graphElement);
        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    private static void AddKeyDeclarations<T>(XElement root, IReadOnlyList<GraphAttribute<T>> attributes, string domain)
    {
        foreach (var attribute in attributes)
        {
            root.Add(new XElement(
                Namespace + "key",
                new XAttribute("id", attribute.Name),
                new XAttribute("for", domain),
                new XAttribute("attr.name", attribute.Name),
                new XAttribute("attr.type", AttributeTypeName(attribute.Type))));
        }
    }

    private static void AddDataElements<T>(XElement element, IReadOnlyList<GraphAttribute<T>> attributes, T item)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.GetValue(item).ToGraphMlText() is { } text)
            {
                element.Add(new XElement(Namespace + "data", new XAttribute("key", attribute.Name), text));
            }
        }
    }

    internal static string AttributeTypeName(GraphAttributeType type) => type switch
    {
        GraphAttributeType.String => "string",
        GraphAttributeType.Bool => "boolean",
        GraphAttributeType.Int => "int",
        GraphAttributeType.Long => "long",
        GraphAttributeType.Float => "float",
        _ => "double",
    };
}
