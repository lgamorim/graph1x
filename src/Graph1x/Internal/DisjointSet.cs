namespace Graph1x.Internal;

/// <summary>
/// A disjoint-set (union-find) structure with union by rank and path
/// compression, giving effectively constant-time Find/Union.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class DisjointSet<T>
    where T : notnull
{
    private readonly Dictionary<T, T> _parent;
    private readonly Dictionary<T, int> _rank;
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>Initializes an empty structure using the default comparer.</summary>
    public DisjointSet()
        : this(EqualityComparer<T>.Default)
    {
    }

    /// <summary>Initializes an empty structure using <paramref name="comparer"/> to identify elements.</summary>
    public DisjointSet(IEqualityComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _comparer = comparer;
        _parent = new Dictionary<T, T>(comparer);
        _rank = new Dictionary<T, int>(comparer);
    }

    /// <summary>Gets the number of disjoint sets currently tracked.</summary>
    public int SetCount { get; private set; }

    /// <summary>Adds <paramref name="item"/> as a new singleton set.</summary>
    /// <returns><see langword="true"/> if added; <see langword="false"/> if already tracked.</returns>
    public bool MakeSet(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_parent.ContainsKey(item))
        {
            return false;
        }

        _parent[item] = item;
        _rank[item] = 0;
        SetCount++;
        return true;
    }

    /// <summary>Finds the representative of the set containing <paramref name="item"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not tracked.</exception>
    public T Find(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_parent.TryGetValue(item, out var parent))
        {
            throw new ArgumentException($"Item '{item}' is not in any set.", nameof(item));
        }

        if (_comparer.Equals(parent, item))
        {
            return item;
        }

        var root = Find(parent);
        _parent[item] = root; // path compression
        return root;
    }

    /// <summary>Merges the sets containing <paramref name="first"/> and <paramref name="second"/>.</summary>
    /// <returns><see langword="true"/> if two sets were merged; <see langword="false"/> if already the same set.</returns>
    public bool Union(T first, T second)
    {
        var firstRoot = Find(first);
        var secondRoot = Find(second);
        if (_comparer.Equals(firstRoot, secondRoot))
        {
            return false;
        }

        var firstRank = _rank[firstRoot];
        var secondRank = _rank[secondRoot];
        if (firstRank < secondRank)
        {
            (firstRoot, secondRoot) = (secondRoot, firstRoot);
        }

        _parent[secondRoot] = firstRoot;
        if (firstRank == secondRank)
        {
            _rank[firstRoot] = firstRank + 1;
        }

        SetCount--;
        return true;
    }

    /// <summary>Determines whether both items belong to the same set.</summary>
    public bool AreConnected(T first, T second)
        => _comparer.Equals(Find(first), Find(second));
}
