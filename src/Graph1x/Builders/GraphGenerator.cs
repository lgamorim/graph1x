using Graph1x.Edges;

namespace Graph1x.Builders;

/// <summary>
/// Deterministic graph generators over integer vertices (0-based), for test
/// fixtures, demos, and benchmarks. Random generators take an explicit seed,
/// so the same call always produces the same graph.
/// </summary>
public static class GraphGenerator
{
    /// <summary>Generates an Erdős–Rényi G(n, p) undirected random graph: each unordered pair becomes an edge with probability <paramref name="edgeProbability"/>.</summary>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <param name="edgeProbability">The independent probability of each edge, in [0, 1].</param>
    /// <param name="seed">The random seed; equal seeds produce equal graphs.</param>
    /// <returns>The generated graph. No self-loops or parallel edges.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Arguments are negative or the probability leaves [0, 1].</exception>
    public static UndirectedGraph<int, Edge<int>> ErdosRenyi(int vertexCount, double edgeProbability, int seed)
    {
        ValidateProbability(edgeProbability);
        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), vertexCount);
        var random = new Random(seed);
        for (var i = 0; i < vertexCount; i++)
        {
            for (var j = i + 1; j < vertexCount; j++)
            {
                if (random.NextDouble() < edgeProbability)
                {
                    graph.AddEdge(new Edge<int>(i, j));
                }
            }
        }

        return graph;
    }

    /// <summary>Generates an Erdős–Rényi G(n, p) directed random graph: each ordered pair of distinct vertices becomes an edge with probability <paramref name="edgeProbability"/>.</summary>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <param name="edgeProbability">The independent probability of each edge, in [0, 1].</param>
    /// <param name="seed">The random seed; equal seeds produce equal graphs.</param>
    /// <returns>The generated graph. No self-loops or parallel edges.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Arguments are negative or the probability leaves [0, 1].</exception>
    public static DirectedGraph<int, Edge<int>> ErdosRenyiDirected(int vertexCount, double edgeProbability, int seed)
    {
        ValidateProbability(edgeProbability);
        var graph = WithVertices<DirectedGraph<int, Edge<int>>>(new(), vertexCount);
        var random = new Random(seed);
        for (var i = 0; i < vertexCount; i++)
        {
            for (var j = 0; j < vertexCount; j++)
            {
                if (i != j && random.NextDouble() < edgeProbability)
                {
                    graph.AddEdge(new Edge<int>(i, j));
                }
            }
        }

        return graph;
    }

    /// <summary>Generates the complete graph K(n): every pair of vertices adjacent.</summary>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="vertexCount"/> is negative.</exception>
    public static UndirectedGraph<int, Edge<int>> Complete(int vertexCount)
    {
        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), vertexCount);
        for (var i = 0; i < vertexCount; i++)
        {
            for (var j = i + 1; j < vertexCount; j++)
            {
                graph.AddEdge(new Edge<int>(i, j));
            }
        }

        return graph;
    }

    /// <summary>Generates the complete bipartite graph K(left, right): vertices 0..left-1 each adjacent to every vertex left..left+right-1.</summary>
    /// <param name="leftCount">The size of the left side.</param>
    /// <param name="rightCount">The size of the right side.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either count is negative.</exception>
    public static UndirectedGraph<int, Edge<int>> CompleteBipartite(int leftCount, int rightCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(leftCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rightCount);
        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), checked(leftCount + rightCount));
        for (var i = 0; i < leftCount; i++)
        {
            for (var j = 0; j < rightCount; j++)
            {
                graph.AddEdge(new Edge<int>(i, leftCount + j));
            }
        }

        return graph;
    }

    /// <summary>Generates the path graph P(n): vertices chained 0-1-...-(n-1).</summary>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="vertexCount"/> is negative.</exception>
    public static UndirectedGraph<int, Edge<int>> Path(int vertexCount)
    {
        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), vertexCount);
        for (var i = 1; i < vertexCount; i++)
        {
            graph.AddEdge(new Edge<int>(i - 1, i));
        }

        return graph;
    }

    /// <summary>Generates the cycle graph C(n): the path graph closed back to vertex 0. Requires at least three vertices (a simple graph admits no shorter cycle).</summary>
    /// <param name="vertexCount">The number of vertices, at least 3.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="vertexCount"/> is below 3.</exception>
    public static UndirectedGraph<int, Edge<int>> Cycle(int vertexCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertexCount, 3);
        var graph = Path(vertexCount);
        graph.AddEdge(new Edge<int>(vertexCount - 1, 0));
        return graph;
    }

    /// <summary>Generates the star graph S(n): center vertex 0 adjacent to leaves 1..n.</summary>
    /// <param name="leafCount">The number of leaves.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="leafCount"/> is negative.</exception>
    public static UndirectedGraph<int, Edge<int>> Star(int leafCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(leafCount);
        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), checked(leafCount + 1));
        for (var leaf = 1; leaf <= leafCount; leaf++)
        {
            graph.AddEdge(new Edge<int>(0, leaf));
        }

        return graph;
    }

    /// <summary>Generates the width × height grid graph; vertex ids are y * width + x. Either dimension being zero yields the empty graph.</summary>
    /// <param name="width">The number of columns.</param>
    /// <param name="height">The number of rows.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either dimension is negative.</exception>
    public static UndirectedGraph<int, Edge<int>> Grid(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        var graph = new UndirectedGraph<int, Edge<int>>();
        if (width == 0 || height == 0)
        {
            return graph;
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var id = (y * width) + x;
                graph.AddVertex(id);
                if (x > 0)
                {
                    graph.AddEdge(new Edge<int>(id - 1, id));
                }

                if (y > 0)
                {
                    graph.AddEdge(new Edge<int>(id - width, id));
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// Generates a Barabási–Albert preferential-attachment graph: starting
    /// from <paramref name="edgesPerNewVertex"/> edgeless vertices, each new
    /// vertex attaches to that many distinct existing vertices with
    /// probability proportional to their degree (the first arrival attaches
    /// to all initial vertices). The result is connected, simple, and has
    /// exactly m·(n−m) edges.
    /// </summary>
    /// <param name="vertexCount">The number of vertices, above <paramref name="edgesPerNewVertex"/>.</param>
    /// <param name="edgesPerNewVertex">The number of edges each new vertex brings, at least 1.</param>
    /// <param name="seed">The random seed; equal seeds produce equal graphs.</param>
    /// <returns>The generated graph. No self-loops or parallel edges.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="edgesPerNewVertex"/> is below 1 or not below <paramref name="vertexCount"/>.</exception>
    public static UndirectedGraph<int, Edge<int>> BarabasiAlbert(int vertexCount, int edgesPerNewVertex, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(edgesPerNewVertex, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(edgesPerNewVertex, vertexCount);

        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), vertexCount);
        var random = new Random(seed);

        // One entry per degree unit: sampling uniformly from this list is
        // sampling vertices proportionally to their degree.
        var attachmentPool = new List<int>(2 * edgesPerNewVertex * (vertexCount - edgesPerNewVertex));
        var targets = new List<int>(edgesPerNewVertex);
        for (var i = 0; i < edgesPerNewVertex; i++)
        {
            targets.Add(i); // the first arrival attaches to every initial vertex
        }

        for (var source = edgesPerNewVertex; source < vertexCount; source++)
        {
            foreach (var target in targets)
            {
                graph.AddEdge(new Edge<int>(source, target));
                attachmentPool.Add(target);
                attachmentPool.Add(source);
            }

            if (source + 1 == vertexCount)
            {
                break;
            }

            var distinct = new HashSet<int>();
            while (distinct.Count < edgesPerNewVertex)
            {
                distinct.Add(attachmentPool[random.Next(attachmentPool.Count)]);
            }

            targets.Clear();
            targets.AddRange(distinct);
        }

        return graph;
    }

    /// <summary>
    /// Generates a Watts–Strogatz small-world graph: a ring lattice where
    /// every vertex is joined to its <paramref name="nearestNeighbors"/>
    /// nearest neighbors, then each lattice edge is rewired to a uniformly
    /// random non-adjacent target with probability
    /// <paramref name="rewiringProbability"/>. The edge count is always
    /// n·k/2.
    /// </summary>
    /// <param name="vertexCount">The number of vertices, above <paramref name="nearestNeighbors"/>.</param>
    /// <param name="nearestNeighbors">The lattice degree; even and non-negative.</param>
    /// <param name="rewiringProbability">The per-edge rewiring probability, in [0, 1].</param>
    /// <param name="seed">The random seed; equal seeds produce equal graphs.</param>
    /// <returns>The generated graph. No self-loops or parallel edges.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nearestNeighbors"/> is negative or not below <paramref name="vertexCount"/>, or the probability leaves [0, 1].</exception>
    /// <exception cref="ArgumentException"><paramref name="nearestNeighbors"/> is odd.</exception>
    public static UndirectedGraph<int, Edge<int>> WattsStrogatz(
        int vertexCount,
        int nearestNeighbors,
        double rewiringProbability,
        int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nearestNeighbors);
        if (nearestNeighbors % 2 != 0)
        {
            throw new ArgumentException(
                "The lattice degree must be even: each vertex joins the same number of neighbors on both sides of the ring.",
                nameof(nearestNeighbors));
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nearestNeighbors, vertexCount);
        ValidateProbability(rewiringProbability);

        var graph = WithVertices<UndirectedGraph<int, Edge<int>>>(new(), vertexCount);
        var random = new Random(seed);
        var half = nearestNeighbors / 2;

        for (var offset = 1; offset <= half; offset++)
        {
            for (var vertex = 0; vertex < vertexCount; vertex++)
            {
                graph.AddEdge(new Edge<int>(vertex, (vertex + offset) % vertexCount));
            }
        }

        // Rewire each lattice edge in place, so the edge count never changes.
        for (var offset = 1; offset <= half; offset++)
        {
            for (var vertex = 0; vertex < vertexCount; vertex++)
            {
                if (random.NextDouble() >= rewiringProbability)
                {
                    continue;
                }

                if (graph.Degree(vertex) >= vertexCount - 1)
                {
                    continue; // saturated: no non-adjacent target exists
                }

                int rewired;
                do
                {
                    rewired = random.Next(vertexCount);
                }
                while (rewired == vertex || graph.ContainsEdge(vertex, rewired));

                graph.RemoveEdge(new Edge<int>(vertex, (vertex + offset) % vertexCount));
                graph.AddEdge(new Edge<int>(vertex, rewired));
            }
        }

        return graph;
    }

    private static TGraph WithVertices<TGraph>(TGraph graph, int vertexCount)
        where TGraph : IMutableGraph<int, Edge<int>>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            graph.AddVertex(vertex);
        }

        return graph;
    }

    private static void ValidateProbability(double edgeProbability)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(edgeProbability);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(edgeProbability, 1.0);
    }
}
