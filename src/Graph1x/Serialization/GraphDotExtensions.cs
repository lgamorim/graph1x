using System.Text;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Exports graphs to the Graphviz DOT language. Output is deterministic
/// (vertices in insertion order, edges in the graph's enumeration order —
/// grouped by source vertex — and <c>\n</c> line endings) so it can be
/// asserted, diffed, and cached; every identifier is quoted and escaped,
/// so arbitrary vertex strings are safe.
/// </summary>
public static class GraphDotExtensions
{
    /// <summary>Renders the graph in DOT with default options.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <returns>The DOT document.</returns>
    public static string ToDot<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => graph.ToDot(new DotExportOptions<TVertex, TEdge>());

    /// <summary>Renders the graph in DOT.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to render.</param>
    /// <param name="options">Rendering options.</param>
    /// <returns>The DOT document: <c>digraph</c>/<c>-&gt;</c> for directed graphs, <c>graph</c>/<c>--</c> for undirected ones.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static string ToDot<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        DotExportOptions<TVertex, TEdge> options)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var vertexId = options.VertexLabel ?? (vertex => vertex.ToString() ?? string.Empty);
        var keyword = graph.IsDirected ? "digraph" : "graph";
        var edgeOperator = graph.IsDirected ? "->" : "--";

        var builder = new StringBuilder();
        builder.Append(keyword).Append(' ').Append(Quote(options.GraphName)).Append(" {\n");

        foreach (var vertex in graph.Vertices)
        {
            builder.Append("    ").Append(Quote(vertexId(vertex))).Append(";\n");
        }

        foreach (var edge in graph.Edges)
        {
            builder.Append("    ")
                .Append(Quote(vertexId(edge.Source)))
                .Append(' ').Append(edgeOperator).Append(' ')
                .Append(Quote(vertexId(edge.Target)));

            if (options.EdgeLabel is not null)
            {
                builder.Append(" [label=").Append(Quote(options.EdgeLabel(edge))).Append(']');
            }

            builder.Append(";\n");
        }

        builder.Append("}\n");
        return builder.ToString();
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    break; // normalize CRLF to the escaped \n emitted above
                default:
                    builder.Append(character);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }
}
