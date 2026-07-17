using System.Globalization;
using System.Text;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Exports graphs to Mermaid flowchart syntax. Output is deterministic
/// (vertices in insertion order, edges in the graph's enumeration order —
/// grouped by source vertex — and <c>\n</c> line endings) so it can be
/// asserted, diffed, and cached. Mermaid node identifiers cannot be arbitrary
/// strings, so nodes get synthetic identifiers (<c>v0</c>, <c>v1</c>, … in
/// insertion order) with the display label attached at declaration; labels
/// are escaped, so arbitrary vertex strings are safe.
/// </summary>
public static class GraphMermaidExtensions
{
    /// <summary>Renders the graph as a Mermaid flowchart with default options.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <returns>The Mermaid document.</returns>
    public static string ToMermaid<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ToMermaid(new MermaidExportOptions<TVertex, TEdge>());

    /// <summary>Renders the graph as a Mermaid flowchart.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <param name="options">Rendering options.</param>
    /// <returns>The Mermaid document: <c>--&gt;</c> edges for directed graphs, <c>---</c> for undirected ones.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static string ToMermaid<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        MermaidExportOptions<TVertex, TEdge> options)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var vertexLabel = options.VertexLabel ?? (vertex => vertex.ToString() ?? string.Empty);
        var edgeOperator = graph.IsDirected ? "-->" : "---";

        var builder = new StringBuilder();
        builder.Append("flowchart ").Append(DirectionCode(options.Direction)).Append('\n');

        var ids = new Dictionary<TVertex, string>(graph.VertexCount, graph.VertexComparer);
        foreach (var vertex in graph.Vertices)
        {
            var id = "v" + ids.Count.ToString(CultureInfo.InvariantCulture);
            ids[vertex] = id;
            builder.Append("    ").Append(id).Append("[\"");
            AppendEscaped(builder, vertexLabel(vertex));
            builder.Append("\"]\n");
        }

        foreach (var edge in graph.Edges)
        {
            builder.Append("    ").Append(ids[edge.Source]).Append(' ').Append(edgeOperator);

            if (options.EdgeLabel is not null)
            {
                builder.Append("|\"");
                AppendEscaped(builder, options.EdgeLabel(edge));
                builder.Append("\"|");
            }

            builder.Append(' ').Append(ids[edge.Target]).Append('\n');
        }

        return builder.ToString();
    }

    private static string DirectionCode(MermaidDirection direction) => direction switch
    {
        MermaidDirection.TopDown => "TD",
        MermaidDirection.LeftToRight => "LR",
        MermaidDirection.BottomToTop => "BT",
        MermaidDirection.RightToLeft => "RL",
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    private static void AppendEscaped(StringBuilder builder, string value)
    {
        foreach (var character in value)
        {
            switch (character)
            {
                case '#':
                    // Mermaid reads '#' as the start of an entity escape, so a
                    // literal one must become an entity itself — otherwise the
                    // label "#quot;" would render as the quote below.
                    builder.Append("#35;");
                    break;
                case '"':
                    builder.Append("#quot;");
                    break;
                case '\n':
                    builder.Append("<br/>");
                    break;
                case '\r':
                    break; // normalize CRLF to the <br/> emitted for \n
                default:
                    builder.Append(character);
                    break;
            }
        }
    }
}
