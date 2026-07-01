using Graph1x.Internal;

namespace Graph1x.UnitTests.Internal;

public class DisjointSetTests
{
    [Fact]
    public void MakeSet_NewItem_ReturnsTrue()
    {
        var sets = new DisjointSet<string>();

        Assert.True(sets.MakeSet("a"));
        Assert.Equal(1, sets.SetCount);
    }

    [Fact]
    public void MakeSet_Duplicate_ReturnsFalse()
    {
        var sets = new DisjointSet<string>();
        sets.MakeSet("a");

        Assert.False(sets.MakeSet("a"));
        Assert.Equal(1, sets.SetCount);
    }

    [Fact]
    public void Find_MissingItem_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DisjointSet<string>().Find("ghost"));
    }

    [Fact]
    public void Find_SingletonSet_ReturnsItself()
    {
        var sets = new DisjointSet<string>();
        sets.MakeSet("a");

        Assert.Equal("a", sets.Find("a"));
    }

    [Fact]
    public void Union_DistinctSets_MergesAndReturnsTrue()
    {
        var sets = new DisjointSet<string>();
        sets.MakeSet("a");
        sets.MakeSet("b");

        Assert.True(sets.Union("a", "b"));
        Assert.True(sets.AreConnected("a", "b"));
        Assert.Equal(1, sets.SetCount);
    }

    [Fact]
    public void Union_AlreadyJoined_ReturnsFalse()
    {
        var sets = new DisjointSet<string>();
        sets.MakeSet("a");
        sets.MakeSet("b");
        sets.Union("a", "b");

        Assert.False(sets.Union("b", "a"));
        Assert.Equal(1, sets.SetCount);
    }

    [Fact]
    public void AreConnected_IsTransitive()
    {
        var sets = new DisjointSet<int>();
        for (var i = 0; i < 5; i++)
        {
            sets.MakeSet(i);
        }

        sets.Union(0, 1);
        sets.Union(1, 2);
        sets.Union(3, 4);

        Assert.True(sets.AreConnected(0, 2));
        Assert.False(sets.AreConnected(2, 3));
        Assert.Equal(2, sets.SetCount);
    }

    [Fact]
    public void Find_IsIdempotentAfterPathCompression()
    {
        var sets = new DisjointSet<int>();
        for (var i = 0; i < 10; i++)
        {
            sets.MakeSet(i);
        }

        for (var i = 1; i < 10; i++)
        {
            sets.Union(i - 1, i);
        }

        var root = sets.Find(9);

        Assert.Equal(root, sets.Find(9));
        Assert.Equal(root, sets.Find(0));
        Assert.Equal(root, sets.Find(5));
    }

    [Fact]
    public void CustomComparer_IdentifiesItemsThroughIt()
    {
        var sets = new DisjointSet<string>(StringComparer.OrdinalIgnoreCase);
        sets.MakeSet("A");
        sets.MakeSet("b");
        sets.Union("a", "B");

        Assert.True(sets.AreConnected("A", "b"));
    }
}
