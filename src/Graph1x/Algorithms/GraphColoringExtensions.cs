using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// Heuristic vertex coloring using DSatur (Brélaz): color the vertex with the
/// highest saturation (most distinct neighbor colors) first, breaking ties by
/// degree. Exact on bipartite graphs; an upper bound in general — computing
/// the chromatic number exactly is NP-hard and out of scope.
/// </summary>
public static class GraphColoringExtensions
{
    /// <summary>
    /// Computes a proper vertex coloring with the DSatur heuristic. Edge
    /// direction is ignored (coloring concerns the underlying undirected
    /// graph); parallel edges are harmless.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to color.</param>
    /// <returns>The color assignment.</returns>
    /// <exception cref="ArgumentException">The graph contains a self-loop, which no proper coloring can satisfy.</exception>
    public static GraphColoring<TVertex> ColorVertices<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        var comparer = graph.VertexComparer;

        // Direction-agnostic, parallel-edge-free neighborhoods.
        var neighbors = new Dictionary<TVertex, HashSet<TVertex>>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            neighbors[vertex] = new HashSet<TVertex>(comparer);
        }

        foreach (var edge in graph.Edges)
        {
            if (comparer.Equals(edge.Source, edge.Target))
            {
                throw new ArgumentException(
                    $"Vertex '{edge.Source}' has a self-loop; no proper coloring exists.", nameof(graph));
            }

            neighbors[edge.Source].Add(edge.Target);
            neighbors[edge.Target].Add(edge.Source);
        }

        var order = graph.Vertices.ToList();
        var colors = new Dictionary<TVertex, int>(comparer);
        var neighborColors = new Dictionary<TVertex, HashSet<int>>(comparer);
        foreach (var vertex in order)
        {
            neighborColors[vertex] = [];
        }

        var colorCount = 0;
        for (var step = 0; step < order.Count; step++)
        {
            // DSatur selection: max saturation, then max degree, then insertion order.
            TVertex? chosen = default;
            var found = false;
            var bestSaturation = -1;
            var bestDegree = -1;
            foreach (var vertex in order)
            {
                if (colors.ContainsKey(vertex))
                {
                    continue;
                }

                var saturation = neighborColors[vertex].Count;
                var degree = neighbors[vertex].Count;
                if (saturation > bestSaturation || (saturation == bestSaturation && degree > bestDegree))
                {
                    chosen = vertex;
                    found = true;
                    bestSaturation = saturation;
                    bestDegree = degree;
                }
            }

            if (!found)
            {
                break;
            }

            var forbidden = neighborColors[chosen!];
            var color = 0;
            while (forbidden.Contains(color))
            {
                color++;
            }

            colors[chosen!] = color;
            colorCount = Math.Max(colorCount, color + 1);
            foreach (var neighbor in neighbors[chosen!])
            {
                neighborColors[neighbor].Add(color);
            }
        }

        return new GraphColoring<TVertex>(colors, colorCount);
    }
}
