namespace Graph1x.Algorithms;

/// <summary>
/// Thrown when an operation that requires an acyclic graph (such as
/// topological sorting) encounters a cycle.
/// </summary>
public class GraphCycleException : InvalidOperationException
{
    /// <summary>Initializes the exception with a default message.</summary>
    public GraphCycleException()
        : base("The graph contains a cycle.")
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/>.</summary>
    /// <param name="message">The error message.</param>
    public GraphCycleException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/> and <paramref name="innerException"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public GraphCycleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/> and the offending <paramref name="cycle"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="cycle">The vertices of the offending cycle, in cycle order.</param>
    public GraphCycleException(string message, IReadOnlyList<object> cycle)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        Cycle = cycle;
    }

    /// <summary>
    /// Gets the vertices of the offending cycle in cycle order (the last vertex
    /// connects back to the first), or an empty list when the cycle was not
    /// captured.
    /// </summary>
    public IReadOnlyList<object> Cycle { get; } = [];
}
