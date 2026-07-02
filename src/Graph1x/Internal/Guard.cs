namespace Graph1x.Internal;

/// <summary>Shared argument-validation helpers.</summary>
internal static class Guard
{
    /// <summary>Throws if <paramref name="vertex"/> is not present in <paramref name="vertices"/>.</summary>
    internal static void VertexExists<TVertex, TValue>(
        Dictionary<TVertex, TValue> vertices,
        TVertex vertex,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(vertex))] string? paramName = null)
        where TVertex : notnull
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        if (!vertices.ContainsKey(vertex))
        {
            throw new ArgumentException($"Vertex '{vertex}' is not in the graph.", paramName);
        }
    }
}
