using Graph1x.Edges;

namespace Graph1x.Hypergraphs;

/// <summary>
/// Expansions from hypergraphs into binary-edge graphs, unlocking the entire
/// algorithm suite (components, shortest paths, DOT export, ...) for
/// hypergraph data without hypergraph-specific algorithm code.
/// </summary>
public static class HypergraphExpansionExtensions
{
    /// <summary>
    /// Builds the clique expansion (2-section): an undirected simple graph on
    /// the same vertices where two vertices are adjacent when any hyperedge
    /// contains both. Overlapping hyperedges collapse to one edge; singleton
    /// hyperedges contribute no edges.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="hypergraph">The hypergraph to expand.</param>
    /// <returns>The clique expansion.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hypergraph"/> is <see langword="null"/>.</exception>
    public static UndirectedGraph<TVertex, Edge<TVertex>> ToCliqueExpansion<TVertex>(
        this Hypergraph<TVertex> hypergraph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(hypergraph);

        var expansion = new UndirectedGraph<TVertex, Edge<TVertex>>(hypergraph.VertexComparer);
        foreach (var vertex in hypergraph.Vertices)
        {
            expansion.AddVertex(vertex);
        }

        foreach (var hyperedge in hypergraph.Hyperedges)
        {
            var members = hyperedge.Vertices.ToList();
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    expansion.AddEdge(new Edge<TVertex>(members[i], members[j]));
                }
            }
        }

        return expansion;
    }

    /// <summary>
    /// Builds the bipartite incidence expansion: vertex nodes on one side,
    /// one node per hyperedge on the other, and an edge for every membership.
    /// Unlike the clique expansion this is lossless — duplicate hyperedges
    /// keep distinct nodes, and hyperedge membership is exactly recoverable.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="hypergraph">The hypergraph to expand.</param>
    /// <returns>The bipartite incidence graph over <see cref="IncidenceVertex{TVertex}"/> nodes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hypergraph"/> is <see langword="null"/>.</exception>
    public static UndirectedGraph<IncidenceVertex<TVertex>, Edge<IncidenceVertex<TVertex>>> ToBipartiteIncidenceGraph<TVertex>(
        this Hypergraph<TVertex> hypergraph)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(hypergraph);

        var comparer = new IncidenceVertexComparer<TVertex>(hypergraph.VertexComparer);
        var incidence = new UndirectedGraph<IncidenceVertex<TVertex>, Edge<IncidenceVertex<TVertex>>>(comparer);

        foreach (var vertex in hypergraph.Vertices)
        {
            incidence.AddVertex(IncidenceVertex.ForVertex(vertex));
        }

        var index = 0;
        foreach (var hyperedge in hypergraph.Hyperedges)
        {
            var hyperedgeNode = IncidenceVertex.ForHyperedge<TVertex>(index++);
            incidence.AddVertex(hyperedgeNode);
            foreach (var member in hyperedge.Vertices)
            {
                incidence.AddEdge(new Edge<IncidenceVertex<TVertex>>(IncidenceVertex.ForVertex(member), hyperedgeNode));
            }
        }

        return incidence;
    }

    /// <summary>Equality over incidence nodes that honors the hypergraph's vertex comparer.</summary>
    private sealed class IncidenceVertexComparer<TVertex>(IEqualityComparer<TVertex> vertexComparer)
        : IEqualityComparer<IncidenceVertex<TVertex>>
        where TVertex : notnull
    {
        public bool Equals(IncidenceVertex<TVertex> x, IncidenceVertex<TVertex> y)
        {
            if (x.IsHyperedge != y.IsHyperedge)
            {
                return false;
            }

            return x.IsHyperedge
                ? x.HyperedgeIndex == y.HyperedgeIndex
                : vertexComparer.Equals(x.Vertex, y.Vertex);
        }

        public int GetHashCode(IncidenceVertex<TVertex> obj)
            => obj.IsHyperedge ? obj.HyperedgeIndex : vertexComparer.GetHashCode(obj.Vertex);
    }
}
