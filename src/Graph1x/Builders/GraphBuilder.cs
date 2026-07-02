using Graph1x.Edges;

namespace Graph1x.Builders;

/// <summary>
/// Fluent builder over any mutable graph. Obtain one from the factory methods
/// on <see cref="Graph"/> (or wrap an existing instance with
/// <see cref="Graph.Wrap{TVertex, TEdge}"/>), chain additions, then call
/// <see cref="Build"/> to get the typed graph back.
/// </summary>
/// <typeparam name="TGraph">The concrete graph type being built.</typeparam>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TEdge">The edge type.</typeparam>
public sealed class GraphBuilder<TGraph, TVertex, TEdge>
    where TGraph : IMutableGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex>
{
    private readonly TGraph _graph;

    internal GraphBuilder(TGraph graph)
    {
        _graph = graph;
    }

    /// <summary>Adds a vertex; duplicates are ignored.</summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>This builder, for chaining.</returns>
    public GraphBuilder<TGraph, TVertex, TEdge> AddVertex(TVertex vertex)
    {
        _graph.AddVertex(vertex);
        return this;
    }

    /// <summary>Adds several vertices; duplicates are ignored.</summary>
    /// <param name="vertices">The vertices to add.</param>
    /// <returns>This builder, for chaining.</returns>
    public GraphBuilder<TGraph, TVertex, TEdge> AddVertices(params IEnumerable<TVertex> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        foreach (var vertex in vertices)
        {
            _graph.AddVertex(vertex);
        }

        return this;
    }

    /// <summary>Adds an edge, auto-adding missing endpoints; edges the graph forbids are ignored.</summary>
    /// <param name="edge">The edge to add.</param>
    /// <returns>This builder, for chaining.</returns>
    public GraphBuilder<TGraph, TVertex, TEdge> AddEdge(TEdge edge)
    {
        _graph.AddEdge(edge);
        return this;
    }

    /// <summary>Adds several edges, auto-adding missing endpoints.</summary>
    /// <param name="edges">The edges to add.</param>
    /// <returns>This builder, for chaining.</returns>
    public GraphBuilder<TGraph, TVertex, TEdge> AddEdges(params IEnumerable<TEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        foreach (var edge in edges)
        {
            _graph.AddEdge(edge);
        }

        return this;
    }

    /// <summary>Returns the built graph.</summary>
    /// <returns>The populated graph instance.</returns>
    public TGraph Build() => _graph;
}
