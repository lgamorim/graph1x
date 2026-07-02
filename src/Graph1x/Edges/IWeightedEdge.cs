using System.Numerics;

namespace Graph1x.Edges;

/// <summary>
/// An edge carrying a numeric weight. Any <see cref="INumber{TSelf}"/> works as
/// the weight type (int, double, decimal, ...), so algorithms can compute over
/// weights via generic math without boxing or conversions.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The numeric weight type.</typeparam>
public interface IWeightedEdge<TVertex, TWeight> : IEdge<TVertex>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    /// <summary>Gets the weight of the edge.</summary>
    TWeight Weight { get; }
}
