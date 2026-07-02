using System.Numerics;
using Graph1x.Edges;

namespace Graph1x.Builders;

/// <summary>
/// Factory entry points for fluently building graphs.
/// </summary>
public static class Graph
{
    /// <summary>Starts building a simple directed graph with unweighted edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <returns>A builder over a fresh <see cref="DirectedGraph{TVertex, TEdge}"/>.</returns>
    public static GraphBuilder<DirectedGraph<TVertex, Edge<TVertex>>, TVertex, Edge<TVertex>> Directed<TVertex>()
        where TVertex : notnull
        => new(new DirectedGraph<TVertex, Edge<TVertex>>());

    /// <summary>Starts building a simple undirected graph with unweighted edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <returns>A builder over a fresh <see cref="UndirectedGraph{TVertex, TEdge}"/>.</returns>
    public static GraphBuilder<UndirectedGraph<TVertex, Edge<TVertex>>, TVertex, Edge<TVertex>> Undirected<TVertex>()
        where TVertex : notnull
        => new(new UndirectedGraph<TVertex, Edge<TVertex>>());

    /// <summary>Starts building a simple directed graph with weighted edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <returns>A builder over a fresh <see cref="DirectedGraph{TVertex, TEdge}"/>.</returns>
    public static GraphBuilder<DirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>>, TVertex, WeightedEdge<TVertex, TWeight>> DirectedWeighted<TVertex, TWeight>()
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => new(new DirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>>());

    /// <summary>Starts building a simple undirected graph with weighted edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TWeight">The numeric weight type.</typeparam>
    /// <returns>A builder over a fresh <see cref="UndirectedGraph{TVertex, TEdge}"/>.</returns>
    public static GraphBuilder<UndirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>>, TVertex, WeightedEdge<TVertex, TWeight>> UndirectedWeighted<TVertex, TWeight>()
        where TVertex : notnull
        where TWeight : INumber<TWeight>
        => new(new UndirectedGraph<TVertex, WeightedEdge<TVertex, TWeight>>());

    /// <summary>Starts building on top of an existing mutable graph (multigraph, DAG, matrix-backed, ...).</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to populate.</param>
    /// <returns>A builder over <paramref name="graph"/>; <c>Build()</c> returns the same instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static GraphBuilder<IMutableGraph<TVertex, TEdge>, TVertex, TEdge> Wrap<TVertex, TEdge>(
        IMutableGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        return new GraphBuilder<IMutableGraph<TVertex, TEdge>, TVertex, TEdge>(graph);
    }
}
