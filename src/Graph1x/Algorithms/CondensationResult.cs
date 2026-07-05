using Graph1x.Edges;

namespace Graph1x.Algorithms;

/// <summary>
/// The condensation of a directed graph: every strongly connected component
/// collapsed into one integer vertex, yielding a DAG. Component indexes
/// follow Tarjan's reverse-topological emission order, so every condensation
/// edge points from a higher index to a lower one.
/// </summary>
/// <typeparam name="TVertex">The original vertex type.</typeparam>
public sealed class CondensationResult<TVertex>
    where TVertex : notnull
{
    private readonly IReadOnlyList<IReadOnlySet<TVertex>> _members;
    private readonly Dictionary<TVertex, int> _componentOf;

    internal CondensationResult(
        IDirectedGraph<int, Edge<int>> graph,
        IReadOnlyList<IReadOnlySet<TVertex>> members,
        Dictionary<TVertex, int> componentOf)
    {
        Graph = graph;
        _members = members;
        _componentOf = componentOf;
    }

    /// <summary>Gets the condensation DAG over component indexes.</summary>
    public IDirectedGraph<int, Edge<int>> Graph { get; }

    /// <summary>Gets the number of strongly connected components.</summary>
    public int ComponentCount => _members.Count;

    /// <summary>Gets the component index containing <paramref name="vertex"/>.</summary>
    /// <param name="vertex">An original graph vertex.</param>
    /// <returns>The zero-based component index.</returns>
    /// <exception cref="ArgumentException"><paramref name="vertex"/> was not part of the condensed graph.</exception>
    public int ComponentOf(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _componentOf.TryGetValue(vertex, out var component)
            ? component
            : throw new ArgumentException($"Vertex '{vertex}' was not part of the condensed graph.", nameof(vertex));
    }

    /// <summary>Gets the original vertices inside component <paramref name="componentIndex"/>.</summary>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The component's vertex set.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="componentIndex"/> is out of range.</exception>
    public IReadOnlySet<TVertex> Members(int componentIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(componentIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(componentIndex, _members.Count);
        return _members[componentIndex];
    }
}
