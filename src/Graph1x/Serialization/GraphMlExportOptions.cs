using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Options controlling <see cref="GraphMlExtensions.ToGraphMl{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge}, GraphMlExportOptions{TVertex, TEdge})"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed record GraphMlExportOptions<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Gets the graph id emitted on the graph element. Defaults to "G".</summary>
    public string GraphId { get; init; } = "G";

    /// <summary>
    /// Gets the function producing a vertex's GraphML node id. Defaults to
    /// <see cref="object.ToString"/>.
    /// </summary>
    public Func<TVertex, string>? VertexId { get; init; }

    /// <summary>
    /// Gets the function reading an edge's weight. When set, a
    /// <c>weight</c> key is declared and every edge carries a data element.
    /// </summary>
    public Func<TEdge, double>? EdgeWeight { get; init; }

    /// <summary>
    /// Gets the vertex attributes to export: each declares a typed
    /// <c>&lt;key for="node"&gt;</c> element and a data element per vertex.
    /// </summary>
    public IReadOnlyList<GraphAttribute<TVertex>> VertexAttributes { get; init; } = [];

    /// <summary>
    /// Gets the edge attributes to export: each declares a typed
    /// <c>&lt;key for="edge"&gt;</c> element and a data element per edge.
    /// </summary>
    public IReadOnlyList<GraphAttribute<TEdge>> EdgeAttributes { get; init; } = [];
}
