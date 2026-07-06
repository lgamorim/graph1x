using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Graph set operations: induced subgraphs, unions, and complements. Every
/// operation returns a new graph matching the source's direction and
/// parallel-edge policy (comparer included); the inputs are never mutated.
/// </summary>
public static class GraphOperationsExtensions
{
    /// <summary>
    /// Builds the subgraph induced by <paramref name="vertices"/>: the selected
    /// vertices that exist in the graph, plus every edge whose endpoints are
    /// both selected. Vertices not present in the graph are ignored.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to take the subgraph of.</param>
    /// <param name="vertices">The vertices to keep.</param>
    /// <returns>A new graph of the same family containing the induced subgraph.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IMutableGraph<TVertex, TEdge> Subgraph<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        IEnumerable<TVertex> vertices)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(vertices);

        var kept = new HashSet<TVertex>(graph.VertexComparer);
        foreach (var vertex in vertices)
        {
            if (graph.ContainsVertex(vertex))
            {
                kept.Add(vertex);
            }
        }

        var subgraph = GraphCloneCore.CreateEmptyLike(graph);
        foreach (var vertex in kept)
        {
            subgraph.AddVertex(vertex);
        }

        foreach (var edge in graph.Edges)
        {
            if (kept.Contains(edge.Source) && kept.Contains(edge.Target))
            {
                subgraph.AddEdge(edge);
            }
        }

        return subgraph;
    }

    /// <summary>
    /// Builds the subgraph induced by <paramref name="vertices"/>, keeping the
    /// <see cref="IDirectedGraph{TVertex, TEdge}"/> static type.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The directed graph to take the subgraph of.</param>
    /// <param name="vertices">The vertices to keep.</param>
    /// <returns>A new directed graph containing the induced subgraph.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IDirectedGraph<TVertex, TEdge> Subgraph<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> graph,
        IEnumerable<TVertex> vertices)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => (IDirectedGraph<TVertex, TEdge>)((IReadOnlyGraph<TVertex, TEdge>)graph).Subgraph(vertices);

    /// <summary>
    /// Builds the union of two graphs: all vertices and all edges of both.
    /// The result's family and comparer come from <paramref name="first"/>;
    /// when the result is a simple graph, edges sharing endpoints collapse
    /// (the copy from <paramref name="first"/> wins).
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="first">The first operand; decides the result's family and comparer.</param>
    /// <param name="second">The second operand; must have the same direction as <paramref name="first"/>.</param>
    /// <returns>A new graph containing the union.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">One graph is directed and the other is not.</exception>
    public static IMutableGraph<TVertex, TEdge> Union<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> first,
        IReadOnlyGraph<TVertex, TEdge> second)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (first.IsDirected != second.IsDirected)
        {
            throw new ArgumentException(
                "The union of a directed and an undirected graph is undefined; make the operands agree on direction.",
                nameof(second));
        }

        var union = GraphCloneCore.Copy(first);
        foreach (var vertex in second.Vertices)
        {
            union.AddVertex(vertex);
        }

        foreach (var edge in second.Edges)
        {
            union.AddEdge(edge);
        }

        return union;
    }

    /// <summary>
    /// Builds the union of two directed graphs, keeping the
    /// <see cref="IDirectedGraph{TVertex, TEdge}"/> static type.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="first">The first operand; decides the result's family and comparer.</param>
    /// <param name="second">The second operand.</param>
    /// <returns>A new directed graph containing the union.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IDirectedGraph<TVertex, TEdge> Union<TVertex, TEdge>(
        this IDirectedGraph<TVertex, TEdge> first,
        IDirectedGraph<TVertex, TEdge> second)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => (IDirectedGraph<TVertex, TEdge>)((IReadOnlyGraph<TVertex, TEdge>)first)
            .Union((IReadOnlyGraph<TVertex, TEdge>)second);

    /// <summary>
    /// Builds the complement of a simple graph: same vertices, and an edge
    /// between two distinct vertices exactly when the original has none.
    /// Self-loops are never emitted (and existing ones simply disappear).
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The simple graph to complement.</param>
    /// <param name="edgeFactory">Builds the complement edge for a (source, target) pair.</param>
    /// <returns>A new graph containing the complement.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The graph allows parallel edges, so its complement is undefined.</exception>
    public static IMutableGraph<TVertex, TEdge> Complement<TVertex, TEdge>(
        this IReadOnlyGraph<TVertex, TEdge> graph,
        Func<TVertex, TVertex, TEdge> edgeFactory)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(edgeFactory);
        if (graph.AllowsParallelEdges)
        {
            throw new ArgumentException(
                "The complement of a multigraph is undefined; complement applies to simple graphs only.",
                nameof(graph));
        }

        var complement = GraphCloneCore.CreateEmptyLike(graph);
        var vertices = new List<TVertex>(graph.VertexCount);
        foreach (var vertex in graph.Vertices)
        {
            complement.AddVertex(vertex);
            vertices.Add(vertex);
        }

        for (var i = 0; i < vertices.Count; i++)
        {
            // Undirected pairs are unordered: start after i to visit each once.
            var start = graph.IsDirected ? 0 : i + 1;
            for (var j = start; j < vertices.Count; j++)
            {
                if (i == j || graph.ContainsEdge(vertices[i], vertices[j]))
                {
                    continue;
                }

                complement.AddEdge(edgeFactory(vertices[i], vertices[j]));
            }
        }

        return complement;
    }

    /// <summary>Builds the complement of a simple graph with <see cref="Edge{TVertex}"/> edges.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="graph">The simple graph to complement.</param>
    /// <returns>A new graph containing the complement.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The graph allows parallel edges, so its complement is undefined.</exception>
    public static IMutableGraph<TVertex, Edge<TVertex>> Complement<TVertex>(
        this IReadOnlyGraph<TVertex, Edge<TVertex>> graph)
        where TVertex : notnull
        => graph.Complement((source, target) => new Edge<TVertex>(source, target));

    /// <summary>
    /// Builds the complement of a simple directed graph, keeping the
    /// <see cref="IDirectedGraph{TVertex, TEdge}"/> static type.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="graph">The simple directed graph to complement.</param>
    /// <returns>A new directed graph containing the complement.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The graph allows parallel edges, so its complement is undefined.</exception>
    public static IDirectedGraph<TVertex, Edge<TVertex>> Complement<TVertex>(
        this IDirectedGraph<TVertex, Edge<TVertex>> graph)
        where TVertex : notnull
        => (IDirectedGraph<TVertex, Edge<TVertex>>)((IReadOnlyGraph<TVertex, Edge<TVertex>>)graph)
            .Complement((source, target) => new Edge<TVertex>(source, target));
}
