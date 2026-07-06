using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Options controlling <see cref="GraphMermaidExtensions.ToMermaid{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge}, MermaidExportOptions{TVertex, TEdge})"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed record MermaidExportOptions<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>
    /// Gets the flow direction emitted in the <c>flowchart</c> header.
    /// Defaults to <see cref="MermaidDirection.TopDown"/>.
    /// </summary>
    public MermaidDirection Direction { get; init; } = MermaidDirection.TopDown;

    /// <summary>
    /// Gets the function producing a vertex's display label. Defaults to
    /// <see cref="object.ToString"/>. Node identifiers are always synthetic
    /// (<c>v0</c>, <c>v1</c>, …); this selector only affects the label text.
    /// </summary>
    public Func<TVertex, string>? VertexLabel { get; init; }

    /// <summary>
    /// Gets the function producing an edge's label. When
    /// <see langword="null"/>, edges are emitted without labels.
    /// </summary>
    public Func<TEdge, string>? EdgeLabel { get; init; }
}
