using Graph1x.Edges;

namespace Graph1x.UnitTests.Edges;

public class WeightedEdgeTests
{
    [Fact]
    public void Constructor_SetsSourceTargetAndWeight()
    {
        var edge = new WeightedEdge<string, double>("a", "b", 2.5);

        Assert.Equal("a", edge.Source);
        Assert.Equal("b", edge.Target);
        Assert.Equal(2.5, edge.Weight);
    }

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeightedEdge<string, int>(null!, "b", 1));
    }

    [Fact]
    public void Constructor_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeightedEdge<string, int>("a", null!, 1));
    }

    [Fact]
    public void Edges_WithSameEndpointsAndWeight_AreEqual()
    {
        var first = new WeightedEdge<string, int>("a", "b", 3);
        var second = new WeightedEdge<string, int>("a", "b", 3);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Edges_WithDifferentWeights_AreNotEqual()
    {
        var cheap = new WeightedEdge<string, int>("a", "b", 1);
        var expensive = new WeightedEdge<string, int>("a", "b", 9);

        Assert.NotEqual(cheap, expensive);
    }

    [Fact]
    public void Weight_SupportsIntegerGenericMath()
    {
        var edges = new[]
        {
            new WeightedEdge<string, int>("a", "b", 1),
            new WeightedEdge<string, int>("b", "c", 2),
        };

        Assert.Equal(3, SumWeights(edges));
    }

    [Fact]
    public void Weight_SupportsDoubleGenericMath()
    {
        var edges = new[]
        {
            new WeightedEdge<string, double>("a", "b", 0.5),
            new WeightedEdge<string, double>("b", "c", 1.25),
        };

        Assert.Equal(1.75, SumWeights(edges));
    }

    [Fact]
    public void Weight_SupportsDecimalGenericMath()
    {
        var edges = new[]
        {
            new WeightedEdge<string, decimal>("a", "b", 0.1m),
            new WeightedEdge<string, decimal>("b", "c", 0.2m),
        };

        Assert.Equal(0.3m, SumWeights(edges));
    }

    [Fact]
    public void NegativeWeights_AreRepresentable()
    {
        var edge = new WeightedEdge<string, int>("a", "b", -4);

        Assert.Equal(-4, edge.Weight);
    }

    [Fact]
    public void Deconstruct_ReturnsSourceTargetAndWeight()
    {
        var (source, target, weight) = new WeightedEdge<string, int>("a", "b", 5);

        Assert.Equal("a", source);
        Assert.Equal("b", target);
        Assert.Equal(5, weight);
    }

    [Fact]
    public void WeightedEdge_ImplementsBothEdgeInterfaces()
    {
        var edge = new WeightedEdge<string, int>("a", "b", 1);

        Assert.IsAssignableFrom<IEdge<string>>(edge);
        Assert.IsAssignableFrom<IWeightedEdge<string, int>>(edge);
    }

    private static TWeight SumWeights<TVertex, TWeight>(IEnumerable<WeightedEdge<TVertex, TWeight>> edges)
        where TVertex : notnull
        where TWeight : System.Numerics.INumber<TWeight>
        => edges.Aggregate(TWeight.Zero, (total, edge) => total + edge.Weight);
}
