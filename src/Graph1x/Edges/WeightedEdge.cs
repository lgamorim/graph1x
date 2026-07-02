using System.Numerics;

namespace Graph1x.Edges;

/// <summary>
/// A weighted edge between two vertices, with value equality over the
/// (source, target, weight) triple.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public readonly record struct WeightedEdge<TVertex, TWeight> : IWeightedEdge<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    /// <summary>Initializes a new weighted edge from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="source">The vertex the edge starts at.</param>
    /// <param name="target">The vertex the edge ends at.</param>
    /// <param name="weight">The weight of the edge.</param>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null"/>.</exception>
    public WeightedEdge(TVertex source, TVertex target, TWeight weight)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Source = source;
        Target = target;
        Weight = weight;
    }

    /// <inheritdoc/>
    public TVertex Source { get; }

    /// <inheritdoc/>
    public TVertex Target { get; }

    /// <inheritdoc/>
    public TWeight Weight { get; }

    /// <summary>Deconstructs the edge into its endpoints and weight.</summary>
    /// <param name="source">The vertex the edge starts at.</param>
    /// <param name="target">The vertex the edge ends at.</param>
    /// <param name="weight">The weight of the edge.</param>
    public void Deconstruct(out TVertex source, out TVertex target, out TWeight weight)
    {
        source = Source;
        target = Target;
        weight = Weight;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Source} -> {Target} ({Weight})";
}
