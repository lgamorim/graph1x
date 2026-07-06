using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Options controlling <see cref="GraphJsonExtensions.ToJson{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge}, GraphJsonExportOptions{TVertex, TEdge})"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed record GraphJsonExportOptions<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>
    /// Gets the function producing a vertex's node id. Defaults to
    /// <see cref="object.ToString"/>.
    /// </summary>
    public Func<TVertex, string>? VertexId { get; init; }

    /// <summary>
    /// Gets the function reading an edge's weight. When set, every edge
    /// object carries a <c>weight</c> property.
    /// </summary>
    public Func<TEdge, double>? EdgeWeight { get; init; }

    /// <summary>
    /// Gets the vertex attributes to export: each writes a typed property
    /// on every node object.
    /// </summary>
    public IReadOnlyList<GraphAttribute<TVertex>> VertexAttributes { get; init; } = [];

    /// <summary>
    /// Gets the edge attributes to export: each writes a typed property on
    /// every edge object.
    /// </summary>
    public IReadOnlyList<GraphAttribute<TEdge>> EdgeAttributes { get; init; } = [];
}
