using Graph1x;
using Graph1x.Algorithms;
using Graph1x.Builders;
using Graph1x.Edges;

namespace Graph1x.UnitTests.Algorithms;

public class CliqueTests
{
    [Fact]
    public void EnumerateMaximalCliques_EmptyGraph_YieldsNothing()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();

        Assert.Empty(graph.EnumerateMaximalCliques());
    }

    [Fact]
    public void EnumerateMaximalCliques_SingleVertex_YieldsThatVertex()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddVertex("a");

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(["a"], clique);
    }

    [Fact]
    public void EnumerateMaximalCliques_Triangle_YieldsSingleClique()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("a", "c"));

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(new[] { "a", "b", "c" }, clique.Order());
    }

    [Fact]
    public void EnumerateMaximalCliques_CompleteGraph_YieldsExactlyOneClique()
    {
        var graph = GraphGenerator.Complete(6);

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(Enumerable.Range(0, 6), clique.Order());
    }

    [Fact]
    public void EnumerateMaximalCliques_PathGraph_YieldsEveryEdge()
    {
        var graph = GraphGenerator.Path(4); // 0-1-2-3

        var cliques = graph.EnumerateMaximalCliques()
            .Select(clique => clique.Order().ToArray())
            .OrderBy(clique => clique[0])
            .ToList();

        Assert.Equal(3, cliques.Count);
        Assert.Equal([0, 1], cliques[0]);
        Assert.Equal([1, 2], cliques[1]);
        Assert.Equal([2, 3], cliques[2]);
    }

    [Fact]
    public void EnumerateMaximalCliques_TwoDisjointTriangles_YieldsBoth()
    {
        var graph = new UndirectedGraph<int, Edge<int>>();
        graph.AddEdge(new Edge<int>(0, 1));
        graph.AddEdge(new Edge<int>(1, 2));
        graph.AddEdge(new Edge<int>(0, 2));
        graph.AddEdge(new Edge<int>(3, 4));
        graph.AddEdge(new Edge<int>(4, 5));
        graph.AddEdge(new Edge<int>(3, 5));

        var cliques = graph.EnumerateMaximalCliques()
            .Select(clique => clique.Order().ToArray())
            .OrderBy(clique => clique[0])
            .ToList();

        Assert.Equal(2, cliques.Count);
        Assert.Equal([0, 1, 2], cliques[0]);
        Assert.Equal([3, 4, 5], cliques[1]);
    }

    [Fact]
    public void EnumerateMaximalCliques_OverlappingCliques_YieldsEachMaximalOne()
    {
        // Two triangles sharing the edge 1-2: {0,1,2} and {1,2,3}.
        var graph = new UndirectedGraph<int, Edge<int>>();
        graph.AddEdge(new Edge<int>(0, 1));
        graph.AddEdge(new Edge<int>(0, 2));
        graph.AddEdge(new Edge<int>(1, 2));
        graph.AddEdge(new Edge<int>(1, 3));
        graph.AddEdge(new Edge<int>(2, 3));

        var cliques = graph.EnumerateMaximalCliques()
            .Select(clique => clique.Order().ToArray())
            .OrderBy(clique => clique[0])
            .ToList();

        Assert.Equal(2, cliques.Count);
        Assert.Equal([0, 1, 2], cliques[0]);
        Assert.Equal([1, 2, 3], cliques[1]);
    }

    [Fact]
    public void EnumerateMaximalCliques_SelfLoop_IsIgnored()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "a"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(new[] { "a", "b" }, clique.Order());
    }

    [Fact]
    public void EnumerateMaximalCliques_MultigraphParallelEdges_CountNeighborsOnce()
    {
        var graph = new UndirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(new[] { "a", "b" }, clique.Order());
    }

    [Fact]
    public void EnumerateMaximalCliques_DirectedGraph_IgnoresDirection()
    {
        // A directed 3-cycle is an undirected triangle: one maximal clique.
        var graph = new DirectedGraph<int, Edge<int>>();
        graph.AddEdge(new Edge<int>(0, 1));
        graph.AddEdge(new Edge<int>(1, 2));
        graph.AddEdge(new Edge<int>(2, 0));

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal([0, 1, 2], clique.Order());
    }

    [Fact]
    public void EnumerateMaximalCliques_IsDeterministic()
    {
        var graph = GraphGenerator.BarabasiAlbert(60, 3, seed: 42);

        var first = graph.EnumerateMaximalCliques().Select(clique => string.Join(",", clique)).ToList();
        var second = graph.EnumerateMaximalCliques().Select(clique => string.Join(",", clique)).ToList();

        Assert.NotEmpty(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void EnumerateMaximalCliques_CoversEveryVertexAndEdge()
    {
        // Every vertex and every non-loop edge lies in some maximal clique.
        var graph = GraphGenerator.WattsStrogatz(40, 4, 0.3, seed: 7);

        var cliques = graph.EnumerateMaximalCliques().Select(clique => clique.ToHashSet()).ToList();

        foreach (var vertex in graph.Vertices)
        {
            Assert.Contains(cliques, clique => clique.Contains(vertex));
        }

        foreach (var edge in graph.Edges)
        {
            Assert.Contains(
                cliques,
                clique => clique.Contains(edge.Source) && clique.Contains(edge.Target));
        }

        // And every reported clique is complete and maximal.
        foreach (var clique in cliques)
        {
            var members = clique.ToList();
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    Assert.True(
                        graph.ContainsEdge(members[i], members[j])
                        || graph.ContainsEdge(members[j], members[i]));
                }
            }
        }
    }

    [Fact]
    public void EnumerateMaximalCliques_IsLazy_FirstCliqueWithoutFullEnumeration()
    {
        var graph = GraphGenerator.BarabasiAlbert(2000, 4, seed: 11);

        var first = graph.EnumerateMaximalCliques().First();

        Assert.NotEmpty(first);
    }

    [Fact]
    public void EnumerateMaximalCliques_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ((UndirectedGraph<string, Edge<string>>)null!).EnumerateMaximalCliques());
    }

    [Fact]
    public void EnumerateMaximalCliques_MatrixGraph_WorksThroughTheSameContract()
    {
        var graph = new UndirectedAdjacencyMatrixGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("b", "c"));
        graph.AddEdge(new Edge<string>("a", "c"));

        var clique = Assert.Single(graph.EnumerateMaximalCliques());
        Assert.Equal(new[] { "a", "b", "c" }, clique.Order());
    }
}
