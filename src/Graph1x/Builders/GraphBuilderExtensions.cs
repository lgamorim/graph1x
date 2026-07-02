using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Builders;

/// <summary>
/// Endpoint-based sugar for builders whose edge type is the library's own
/// <see cref="Edge{TVertex}"/> or <see cref="WeightedEdge{TVertex, TWeight}"/>.
/// </summary>
public static class GraphBuilderExtensions
{
    /// <summary>Adds an unweighted edge given its endpoints.</summary>
    /// <typeparam name="TGraph">The concrete graph type being built.</typeparam>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="builder">The builder to extend.</param>
    /// <param name="source">The edge's source vertex.</param>
    /// <param name="target">The edge's target vertex.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static GraphBuilder<TGraph, TVertex, Edge<TVertex>> AddEdge<TGraph, TVertex>(
        this GraphBuilder<TGraph, TVertex, Edge<TVertex>> builder,
        TVertex source,
        TVertex target)
        where TGraph : IMutableGraph<TVertex, Edge<TVertex>>
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEdge(new Edge<TVertex>(source, target));
    }

    /// <summary>Adds a weighted edge given its endpoints and weight.</summary>
    /// <typeparam name="TGraph">The concrete graph type being built.</typeparam>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <param name="builder">The builder to extend.</param>
    /// <param name="source">The edge's source vertex.</param>
    /// <param name="target">The edge's target vertex.</param>
    /// <param name="weight">The edge's weight.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static GraphBuilder<TGraph, TVertex, WeightedEdge<TVertex, TWeight>> AddEdge<TGraph, TVertex, TWeight>(
        this GraphBuilder<TGraph, TVertex, WeightedEdge<TVertex, TWeight>> builder,
        TVertex source,
        TVertex target,
        TWeight weight)
        where TGraph : IMutableGraph<TVertex, WeightedEdge<TVertex, TWeight>>
        where TVertex : notnull
        where TWeight : INumber<TWeight>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEdge(new WeightedEdge<TVertex, TWeight>(source, target, weight));
    }
}
