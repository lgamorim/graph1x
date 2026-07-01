namespace Graph1x.Algorithms;

/// <summary>
/// Thrown when a shortest-path computation detects a negative-weight cycle,
/// which makes shortest distances undefined for the affected vertices.
/// </summary>
public class NegativeCycleException : InvalidOperationException
{
    /// <summary>Initializes the exception with a default message.</summary>
    public NegativeCycleException()
        : base("The graph contains a negative-weight cycle.")
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/>.</summary>
    /// <param name="message">The error message.</param>
    public NegativeCycleException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with <paramref name="message"/> and <paramref name="innerException"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public NegativeCycleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
