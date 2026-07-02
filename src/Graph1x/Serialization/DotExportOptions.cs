using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Options controlling <see cref="GraphDotExtensions.ToDot{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge}, DotExportOptions{TVertex, TEdge})"/>.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed record DotExportOptions<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    /// <summary>Gets the name emitted in the graph header. Defaults to "G".</summary>
    public string GraphName { get; init; } = "G";

    /// <summary>
    /// Gets the function producing a vertex's DOT identifier. Defaults to
    /// <see cref="object.ToString"/>.
    /// </summary>
    public Func<TVertex, string>? VertexLabel { get; init; }

    /// <summary>
    /// Gets the function producing an edge's label attribute. When
    /// <see langword="null"/>, edges are emitted without attributes.
    /// </summary>
    public Func<TEdge, string>? EdgeLabel { get; init; }
}
