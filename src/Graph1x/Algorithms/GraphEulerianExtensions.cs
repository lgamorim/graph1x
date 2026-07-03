using Graph1x.Edges;
using Graph1x.Internal;

namespace Graph1x.Algorithms;

/// <summary>
/// Eulerian trails: paths and circuits that use every edge exactly once.
/// Existence follows the classic degree conditions (balanced in/out degrees
/// for directed graphs; zero or two odd-degree vertices for undirected ones)
/// plus a single edge-bearing component; construction is an iterative
/// Hierholzer walk. Multigraphs are fully supported — parallel edges are
/// tracked by instance, which is exactly the Königsberg setting.
/// </summary>
public static class GraphEulerianExtensions
{
    /// <summary>Determines whether the graph has an Eulerian circuit (a closed trail using every edge once). Edgeless graphs trivially do.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns><see langword="true"/> if an Eulerian circuit exists.</returns>
    public static bool HasEulerianCircuit<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => Analyze(graph).HasCircuit;

    /// <summary>Determines whether the graph has an Eulerian path (a trail using every edge once; circuits count). Edgeless graphs trivially do.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to inspect.</param>
    /// <returns><see langword="true"/> if an Eulerian path exists.</returns>
    public static bool HasEulerianPath<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
        => Analyze(graph).HasPath;

    /// <summary>Finds an Eulerian circuit, or <see langword="null"/> when none exists.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <returns>The circuit's edge sequence (empty for edgeless graphs), or <see langword="null"/>.</returns>
    public static IReadOnlyList<TEdge>? FindEulerianCircuit<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var analysis = Analyze(graph);
        if (!analysis.HasCircuit)
        {
            return null;
        }

        return analysis.HasEdges ? Hierholzer(graph, analysis.CircuitStart!) : [];
    }

    /// <summary>Finds an Eulerian path, or <see langword="null"/> when none exists.</summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <typeparam name="TEdge">The edge type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <returns>The trail's edge sequence (empty for edgeless graphs), or <see langword="null"/>.</returns>
    public static IReadOnlyList<TEdge>? FindEulerianPath<TVertex, TEdge>(this IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var analysis = Analyze(graph);
        if (!analysis.HasPath)
        {
            return null;
        }

        return analysis.HasEdges ? Hierholzer(graph, analysis.PathStart!) : [];
    }

    private static EulerianAnalysis<TVertex> Analyze<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Every edge must live in one component (isolated vertices are fine).
        var edgeComponents = graph.ConnectedComponents()
            .Count(component => component.Any(vertex => graph.Degree(vertex) > 0));
        if (edgeComponents > 1)
        {
            return new EulerianAnalysis<TVertex>(false, false, default, default, true);
        }

        var hasEdges = graph.EdgeCount > 0;
        if (!hasEdges)
        {
            return new EulerianAnalysis<TVertex>(true, true, default, default, false);
        }

        var anyEdgeVertex = graph.Vertices.First(vertex => graph.Degree(vertex) > 0);

        if (graph is IDirectedGraph<TVertex, TEdge> directed)
        {
            var surplusOut = 0;
            var surplusIn = 0;
            TVertex? start = default;
            foreach (var vertex in directed.Vertices)
            {
                var balance = directed.OutDegree(vertex) - directed.InDegree(vertex);
                switch (balance)
                {
                    case 0:
                        continue;
                    case 1:
                        surplusOut++;
                        start = vertex;
                        break;
                    case -1:
                        surplusIn++;
                        break;
                    default:
                        return new EulerianAnalysis<TVertex>(false, false, default, default, true);
                }
            }

            var balanced = surplusOut == 0 && surplusIn == 0;
            var pathOnly = surplusOut == 1 && surplusIn == 1;
            return new EulerianAnalysis<TVertex>(
                balanced,
                balanced || pathOnly,
                CircuitStart: anyEdgeVertex,
                PathStart: pathOnly ? start : anyEdgeVertex,
                true);
        }

        var oddVertices = graph.Vertices.Where(vertex => graph.Degree(vertex) % 2 == 1).ToList();
        var even = oddVertices.Count == 0;
        var twoOdd = oddVertices.Count == 2;
        return new EulerianAnalysis<TVertex>(
            even,
            even || twoOdd,
            CircuitStart: anyEdgeVertex,
            PathStart: twoOdd ? oddVertices[0] : anyEdgeVertex,
            true);
    }

    /// <summary>
    /// Iterative Hierholzer: walk greedily, and on dead ends pop the vertex
    /// and record its arrival edge; the reversed record is the full trail.
    /// Edge instances are tracked by index, so value-equal parallel edges are
    /// consumed independently.
    /// </summary>
    private static List<TEdge> Hierholzer<TVertex, TEdge>(IReadOnlyGraph<TVertex, TEdge> graph, TVertex start)
        where TVertex : notnull
        where TEdge : IEdge<TVertex>
    {
        var comparer = graph.VertexComparer;
        var isDirected = graph.IsDirected;
        var edges = graph.Edges.ToArray();

        var incidence = new Dictionary<TVertex, List<int>>(comparer);
        foreach (var vertex in graph.Vertices)
        {
            incidence[vertex] = [];
        }

        for (var i = 0; i < edges.Length; i++)
        {
            incidence[edges[i].Source].Add(i);
            if (!isDirected && !comparer.Equals(edges[i].Source, edges[i].Target))
            {
                incidence[edges[i].Target].Add(i);
            }
        }

        var used = new bool[edges.Length];
        var cursor = new Dictionary<TVertex, int>(comparer);
        var trail = new List<TEdge>(edges.Length);
        var stack = new Stack<(TVertex Vertex, int ArrivalEdge)>();
        stack.Push((start, -1));

        while (stack.Count > 0)
        {
            var (vertex, arrival) = stack.Peek();
            var candidates = incidence[vertex];
            var position = cursor.GetValueOrDefault(vertex);
            var advanced = false;

            while (position < candidates.Count)
            {
                var index = candidates[position++];
                if (used[index])
                {
                    continue;
                }

                used[index] = true;
                cursor[vertex] = position;
                var next = isDirected
                    ? edges[index].Target
                    : GraphTraversalCore.OtherEndpoint(graph, edges[index], vertex);
                stack.Push((next, index));
                advanced = true;
                break;
            }

            if (!advanced)
            {
                cursor[vertex] = position;
                stack.Pop();
                if (arrival >= 0)
                {
                    trail.Add(edges[arrival]);
                }
            }
        }

        trail.Reverse();
        return trail;
    }

    private sealed record EulerianAnalysis<TVertex>(
        bool HasCircuit,
        bool HasPath,
        TVertex? CircuitStart,
        TVertex? PathStart,
        bool HasEdges);
}
