using Graph1x;
using Graph1x.Edges;
using Graph1x.Serialization;

namespace Graph1x.UnitTests.Serialization;

public class MermaidExportTests
{
    [Fact]
    public void DirectedGraph_UsesArrowOperator()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var mermaid = graph.ToMermaid();

        Assert.Equal(
            """
            flowchart TD
                v0["a"]
                v1["b"]
                v0 --> v1

            """.ReplaceLineEndings("\n"),
            mermaid);
    }

    [Fact]
    public void UndirectedGraph_UsesTripleDashOperator()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var mermaid = graph.ToMermaid();

        Assert.Contains("v0 --- v1", mermaid);
        Assert.DoesNotContain("-->", mermaid);
    }

    [Fact]
    public void IsolatedVertices_AreDeclared()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("lonely");

        Assert.Contains("v0[\"lonely\"]", graph.ToMermaid());
    }

    [Fact]
    public void EmptyGraph_ProducesHeaderOnly()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Equal("flowchart TD\n", graph.ToMermaid());
    }

    [Fact]
    public void Quotes_AreEscapedAsEntities()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("say \"hi\"");

        Assert.Contains("v0[\"say #quot;hi#quot;\"]", graph.ToMermaid());
    }

    [Fact]
    public void Hashes_AreEscapedAsEntities()
    {
        // '#' introduces Mermaid's entity escapes, so a literal one must be
        // escaped itself or the label renders as something else entirely.
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("C# 13");

        Assert.Contains("v0[\"C#35; 13\"]", graph.ToMermaid());
    }

    [Fact]
    public void HashEscape_DoesNotCollideWithAnEscapedQuote()
    {
        // Distinct labels must produce distinct output: without escaping '#',
        // the literal text "#quot;" renders identically to a real quote.
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("say #quot;hi#quot;");
        graph.AddVertex("say \"hi\"");

        var mermaid = graph.ToMermaid();

        Assert.Contains("v0[\"say #35;quot;hi#35;quot;\"]", mermaid);
        Assert.Contains("v1[\"say #quot;hi#quot;\"]", mermaid);
    }

    [Fact]
    public void Hashes_AreEscapedInEdgeLabels()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        var mermaid = graph.ToMermaid(new MermaidExportOptions<string, Edge<string>>
        {
            EdgeLabel = _ => "#1",
        });

        Assert.Contains("v0 -->|\"#35;1\"| v1", mermaid);
    }

    [Fact]
    public void Newlines_BecomeLineBreakTags()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddVertex("line\nbreak");
        graph.AddVertex("crlf\r\nbreak");

        var mermaid = graph.ToMermaid();

        Assert.Contains("v0[\"line<br/>break\"]", mermaid);
        Assert.Contains("v1[\"crlf<br/>break\"]", mermaid);
    }

    [Fact]
    public void EdgeLabelSelector_EmitsLabeledEdges()
    {
        var graph = new DirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        var mermaid = graph.ToMermaid(new MermaidExportOptions<string, WeightedEdge<string, int>>
        {
            EdgeLabel = edge => edge.Weight.ToString(),
        });

        Assert.Contains("v0 -->|\"7\"| v1", mermaid);
    }

    [Fact]
    public void UndirectedEdgeLabelSelector_EmitsLabeledEdges()
    {
        var graph = new UndirectedGraph<string, WeightedEdge<string, int>>();
        graph.AddEdge(new WeightedEdge<string, int>("a", "b", 7));

        var mermaid = graph.ToMermaid(new MermaidExportOptions<string, WeightedEdge<string, int>>
        {
            EdgeLabel = edge => edge.Weight.ToString(),
        });

        Assert.Contains("v0 ---|\"7\"| v1", mermaid);
    }

    [Fact]
    public void VertexLabelSelector_RewritesLabelsButKeepsIds()
    {
        var graph = new DirectedGraph<int, Edge<int>>();
        graph.AddEdge(new Edge<int>(1, 2));

        var mermaid = graph.ToMermaid(new MermaidExportOptions<int, Edge<int>>
        {
            VertexLabel = vertex => $"node {vertex}",
        });

        Assert.Contains("v0[\"node 1\"]", mermaid);
        Assert.Contains("v1[\"node 2\"]", mermaid);
        Assert.Contains("v0 --> v1", mermaid);
    }

    [Fact]
    public void Direction_IsConfigurable()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.StartsWith("flowchart LR\n", graph.ToMermaid(
            new MermaidExportOptions<string, Edge<string>> { Direction = MermaidDirection.LeftToRight }));
        Assert.StartsWith("flowchart BT\n", graph.ToMermaid(
            new MermaidExportOptions<string, Edge<string>> { Direction = MermaidDirection.BottomToTop }));
        Assert.StartsWith("flowchart RL\n", graph.ToMermaid(
            new MermaidExportOptions<string, Edge<string>> { Direction = MermaidDirection.RightToLeft }));
    }

    [Fact]
    public void Multigraph_EmitsEveryParallelEdge()
    {
        var graph = new DirectedMultigraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));
        graph.AddEdge(new Edge<string>("a", "b"));

        var mermaid = graph.ToMermaid();

        Assert.Equal(2, mermaid.Split("v0 --> v1").Length - 1);
    }

    [Fact]
    public void SelfLoop_IsEmitted()
    {
        var graph = new UndirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "a"));

        Assert.Contains("v0 --- v0", graph.ToMermaid());
    }

    [Fact]
    public void Output_UsesInsertionOrder_Deterministically()
    {
        var graph = new DirectedGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("z", "a"));
        graph.AddEdge(new Edge<string>("a", "m"));

        var mermaid = graph.ToMermaid();

        Assert.Contains("v0[\"z\"]", mermaid);
        Assert.Contains("v1[\"a\"]", mermaid);
        Assert.Contains("v2[\"m\"]", mermaid);
        Assert.True(
            mermaid.IndexOf("v0 --> v1", StringComparison.Ordinal)
            < mermaid.IndexOf("v1 --> v2", StringComparison.Ordinal));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var graph = new DirectedGraph<string, Edge<string>>();

        Assert.Throws<ArgumentNullException>(
            () => ((DirectedGraph<string, Edge<string>>)null!).ToMermaid());
        Assert.Throws<ArgumentNullException>(
            () => graph.ToMermaid(null!));
    }

    [Fact]
    public void MatrixGraph_ExportsThroughTheSameContract()
    {
        var graph = new UndirectedAdjacencyMatrixGraph<string, Edge<string>>();
        graph.AddEdge(new Edge<string>("a", "b"));

        Assert.Contains("v0 --- v1", graph.ToMermaid());
    }
}
