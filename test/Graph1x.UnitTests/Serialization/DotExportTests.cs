using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class DotExportTests
{
    [Fact]
    public void DirectedGraph_UsesDigraphAndArrowOperator()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var dot = graph.ToDot();

        Assert.Equal(
            """
            digraph "G" {
                "a";
                "b";
                "a" -> "b";
            }

            """.ReplaceLineEndings("\n"),
            dot);
    }

    [Fact]
    public void UndirectedGraph_UsesGraphAndDoubleDashOperator()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var dot = graph.ToDot();

        Assert.Contains("graph \"G\" {", dot);
        Assert.DoesNotContain("digraph", dot);
        Assert.Contains("\"a\" -- \"b\";", dot);
    }

    [Fact]
    public void IsolatedVertices_AreDeclared()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("lonely");

        Assert.Contains("\"lonely\";", graph.ToDot());
    }

    [Fact]
    public void EmptyGraph_ProducesEmptyBody()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Equal("digraph \"G\" {\n}\n", graph.ToDot());
    }

    [Fact]
    public void SpecialCharacters_AreEscaped()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("say \"hi\"", "back\\slash"));
        graph.AddVertex("line\nbreak");

        var dot = graph.ToDot();

        Assert.Contains("\"say \\\"hi\\\"\"", dot);
        Assert.Contains("\"back\\\\slash\"", dot);
        Assert.Contains("\"line\\nbreak\"", dot);
    }

    [Fact]
    public void EdgeLabelSelector_EmitsLabelAttributes()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        var dot = graph.ToDot(new DotExportOptions<string, WeightedEdge<string, int>>
        {
            EdgeLabel = edge => edge.Weight.ToString(),
        });

        Assert.Contains("\"a\" -> \"b\" [label=\"7\"];", dot);
    }

    [Fact]
    public void VertexLabelSelector_RewritesVertexIds()
    {
        var graph = new DirectedGraph<int, Edge<int>>();
        graph.AddEdge(new Edge<int>(1, 2));

        var dot = graph.ToDot(new DotExportOptions<int, Edge<int>>
        {
            VertexLabel = vertex => $"v{vertex}",
        });

        Assert.Contains("\"v1\" -> \"v2\";", dot);
        Assert.DoesNotContain("\"1\"", dot);
    }

    [Fact]
    public void GraphName_IsConfigurableAndEscaped()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        var dot = graph.ToDot(new DotExportOptions<string, Edge<string>> { GraphName = "my \"net\"" });

        Assert.StartsWith("digraph \"my \\\"net\\\"\" {", dot);
    }

    [Fact]
    public void Multigraph_EmitsEveryParallelEdge()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var dot = graph.ToDot();

        Assert.Equal(2, dot.Split("\"a\" -> \"b\";").Length - 1);
    }

    [Fact]
    public void SelfLoop_IsEmitted()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Contains("\"a\" -- \"a\";", graph.ToDot());
    }

    [Fact]
    public void Output_UsesInsertionOrder_Deterministically()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("z", "a"));
        graph.AddEdge(new Edge<string>("a", "m"));

        var dot = graph.ToDot();

        Assert.True(dot.IndexOf("\"z\";", StringComparison.Ordinal) < dot.IndexOf("\"a\";", StringComparison.Ordinal));
        Assert.True(
            dot.IndexOf("\"z\" -> \"a\";", StringComparison.Ordinal)
            < dot.IndexOf("\"a\" -> \"m\";", StringComparison.Ordinal));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentNullException>(
            () => ((DirectedGraph<string, Edge<string>>)null!).ToDot());
        Assert.Throws<ArgumentNullException>(
            () => graph.ToDot(null!));
    }

    [Fact]
    public void MatrixGraph_ExportsThroughTheSameContract()
    {
        var graph = new UndirectedAdjacencyMatrixGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Contains("\"a\" -- \"b\";", graph.ToDot());
    }
}
