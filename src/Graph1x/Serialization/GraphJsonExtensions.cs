using System.Text;
using System.Text.Json;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Exports graphs to JSON in the node-link shape used across the ecosystem
/// (NetworkX, D3): <c>{ "directed": ..., "nodes": [...], "edges": [...] }</c>.
/// Written with <see cref="Utf8JsonWriter"/> directly — no reflection-based
/// serialization — with deterministic property order and invariant number
/// formatting.
/// </summary>
public static class GraphJsonExtensions
{
    /// <summary>Renders the graph as node-link JSON with default options.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <returns>The JSON document text.</returns>
    public static string ToJson<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ToJson(new GraphJsonExportOptions<TVertex, TEdge>());

    /// <summary>Renders the graph as node-link JSON.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <param name="options">Rendering options.</param>
    /// <returns>The JSON document text.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static string ToJson<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        GraphJsonExportOptions<TVertex, TEdge> options)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var vertexId = options.VertexId ?? (vertex => vertex.ToString() ?? string.Empty);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("directed", graph.IsDirected);

            writer.WriteStartArray("nodes");
            foreach (var vertex in graph.Vertices)
            {
                writer.WriteStartObject();
                writer.WriteString("id", vertexId(vertex));
                foreach (var attribute in options.VertexAttributes)
                {
                    attribute.GetValue(vertex).WriteTo(writer, attribute.Name);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("edges");
            foreach (var edge in graph.Edges)
            {
                writer.WriteStartObject();
                writer.WriteString("source", vertexId(edge.Source));
                writer.WriteString("target", vertexId(edge.Target));
                if (options.EdgeWeight is not null)
                {
                    writer.WriteNumber("weight", options.EdgeWeight(edge));
                }

                foreach (var attribute in options.EdgeAttributes)
                {
                    attribute.GetValue(edge).WriteTo(writer, attribute.Name);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
