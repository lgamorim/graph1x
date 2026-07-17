namespace Graph1x.Algorithms;

/// <summary>
/// Thrown when an edge weight falls outside the domain an algorithm requires:
/// a negative weight where non-negative ones are needed (Dijkstra, A*, maximum
/// flow), or a non-positive weight where strictly positive ones are needed
/// (weighted betweenness centrality).
/// </summary>
public class NegativeWeightException : InvalidOperationException
{
    /// <summary>Initializes the exception with a default message.</summary>
    public NegativeWeightException()
        : base("The graph contains a negative edge weight.")
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/>.</summary>
    /// <param name="message">The error message.</param>
    public NegativeWeightException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/> and <paramref name="innerException"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public NegativeWeightException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
