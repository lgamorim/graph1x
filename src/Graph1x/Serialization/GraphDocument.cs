using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// A parsed graph document: the structure plus every vertex and edge
/// attribute the source carried. Vertices are strings (the document's node
/// ids); attribute values are typed per the document's declarations —
/// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>,
/// <see cref="long"/>, <see cref="float"/>, or <see cref="double"/> — boxed
/// as <see cref="object"/>.
/// </summary>
public sealed class GraphDocument
{
    internal GraphDocument(
        IMutableGraph<string, Edge<string>> graph,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> vertexData,
        IReadOnlyList<IReadOnlyDictionary<string, object>> edgeData)
    {
        Graph = graph;
        VertexData = vertexData;
        EdgeData = edgeData;
    }

    /// <summary>Gets the parsed graph: a directed or undirected multigraph, the lossless superset.</summary>
    public IMutableGraph<string, Edge<string>> Graph { get; }

    /// <summary>Gets each vertex's attributes, keyed by vertex id then attribute name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> VertexData { get; }

    /// <summary>
    /// Gets each edge's attributes, indexed in document order. Correlate by
    /// document position: <see cref="Graph"/> enumerates edges grouped by
    /// source vertex, which matches document order only when the document's
    /// edges are already grouped that way (as Graph1x's own exporters emit
    /// them) — foreign documents may interleave sources freely.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object>> EdgeData { get; }
}
