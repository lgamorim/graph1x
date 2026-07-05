using System.Globalization;
using System.Xml.Linq;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Exports graphs to GraphML (http://graphml.graphdrawing.org). Vertices are
/// declared first (isolated ones included) and edges follow in insertion
/// order; XML escaping is handled by the writer, so arbitrary vertex strings
/// are safe. Weights are written with the invariant culture.
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
            graphElement.Add(new XElement(Namespace + "node", new XAttribute("id", vertexId(vertex))));
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

        root.Add(graphElement);
        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }
}
