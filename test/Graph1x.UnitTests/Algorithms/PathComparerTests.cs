using Graph1x.Algorithms;

namespace Graph1x.UnitTests.Algorithms;

public class PathComparerTests
{
    private static readonly GraphKShortestPathsExtensions.PathComparer<string> Comparer = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var path = new List<string> { "a", "b" };

        Assert.True(Comparer.Equals(path, path));
    }

    [Fact]
    public void Equals_EitherNull_ReturnsFalse()
    {
        var path = new List<string> { "a" };

        Assert.False(Comparer.Equals(path, null));
        Assert.False(Comparer.Equals(null, path));
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        Assert.False(Comparer.Equals(["a", "b"], ["a", "b", "c"]));
    }

    [Fact]
    public void Equals_SameLengthDifferentVertex_ReturnsFalse()
    {
        Assert.False(Comparer.Equals(["a", "b", "c"], ["a", "x", "c"]));
    }

    [Fact]
    public void Equals_SameSequenceUnderVertexComparer_ReturnsTrueWithEqualHashes()
    {
        List<string> lower = ["a", "b"];
        List<string> upper = ["A", "B"];

        Assert.True(Comparer.Equals(lower, upper));
        Assert.Equal(Comparer.GetHashCode(lower), Comparer.GetHashCode(upper));
    }
}
