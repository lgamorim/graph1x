namespace Graph1x.Hypergraphs;

/// <summary>
/// A hypergraph: vertices connected by hyperedges that may join any number of
/// vertices (at least one). Duplicate vertex sets are allowed — each
/// <see cref="AddHyperedge(IEnumerable{TVertex})"/> call creates a distinct
/// hyperedge. Removing a vertex removes every hyperedge incident to it
/// (strong vertex deletion), mirroring how binary graphs cascade edges.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public sealed class Hypergraph<TVertex>
    where TVertex : notnull
{
    private readonly Dictionary<TVertex, List<Hyperedge<TVertex>>> _incidence;
    private readonly List<Hyperedge<TVertex>> _hyperedges;

    /// <summary>Initializes an empty hypergraph using the default vertex comparer.</summary>
    public Hypergraph()
        : this(EqualityComparer<TVertex>.Default)
    {
    }

    /// <summary>Initializes an empty hypergraph using <paramref name="vertexComparer"/> to identify vertices.</summary>
    /// <param name="vertexComparer">The comparer used to identify vertices.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertexComparer"/> is <see langword="null"/>.</exception>
    public Hypergraph(IEqualityComparer<TVertex> vertexComparer)
    {
        ArgumentNullException.ThrowIfNull(vertexComparer);
        VertexComparer = vertexComparer;
        _incidence = new Dictionary<TVertex, List<Hyperedge<TVertex>>>(vertexComparer);
        _hyperedges = [];
    }

    /// <summary>Gets the comparer used to identify vertices.</summary>
    public IEqualityComparer<TVertex> VertexComparer { get; }

    /// <summary>Gets the number of vertices.</summary>
    public int VertexCount => _incidence.Count;

    /// <summary>Gets the number of hyperedges.</summary>
    public int HyperedgeCount => _hyperedges.Count;

    /// <summary>Gets the vertices of the hypergraph.</summary>
    public IEnumerable<TVertex> Vertices => _incidence.Keys;

    /// <summary>Gets the hyperedges of the hypergraph.</summary>
    public IEnumerable<Hyperedge<TVertex>> Hyperedges => _hyperedges;

    /// <summary>Adds <paramref name="vertex"/> to the hypergraph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns><see langword="true"/> if added; <see langword="false"/> if it was already present.</returns>
    public bool AddVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (_incidence.ContainsKey(vertex))
        {
            return false;
        }

        _incidence.Add(vertex, []);
        return true;
    }

    /// <summary>
    /// Adds a hyperedge joining <paramref name="vertices"/> (duplicates within
    /// the set are collapsed), auto-adding missing vertices.
    /// </summary>
    /// <param name="vertices">The vertices to join; at least one is required.</param>
    /// <returns>The created hyperedge, usable as a handle for removal.</returns>
    /// <exception cref="ArgumentException"><paramref name="vertices"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="vertices"/> or any vertex is <see langword="null"/>.</exception>
    public Hyperedge<TVertex> AddHyperedge(params IEnumerable<TVertex> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        var members = new HashSet<TVertex>(VertexComparer);
        foreach (var vertex in vertices)
        {
            ArgumentNullException.ThrowIfNull(vertex, nameof(vertices));
            members.Add(vertex);
        }

        if (members.Count == 0)
        {
            throw new ArgumentException("A hyperedge must join at least one vertex.", nameof(vertices));
        }

        var hyperedge = new Hyperedge<TVertex>(members);
        foreach (var vertex in members)
        {
            AddVertex(vertex);
            _incidence[vertex].Add(hyperedge);
        }

        _hyperedges.Add(hyperedge);
        return hyperedge;
    }

    /// <summary>Removes <paramref name="vertex"/> and every hyperedge incident to it.</summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if it was not present.</returns>
    public bool RemoveVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!_incidence.TryGetValue(vertex, out var incident))
        {
            return false;
        }

        foreach (var hyperedge in incident)
        {
            _hyperedges.Remove(hyperedge);
            foreach (var member in hyperedge.Vertices)
            {
                if (!VertexComparer.Equals(member, vertex))
                {
                    _incidence[member].Remove(hyperedge);
                }
            }
        }

        _incidence.Remove(vertex);
        return true;
    }

    /// <summary>Removes <paramref name="hyperedge"/> from the hypergraph. Its vertices stay.</summary>
    /// <param name="hyperedge">The hyperedge handle to remove.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if it was not present.</returns>
    public bool RemoveHyperedge(Hyperedge<TVertex> hyperedge)
    {
        ArgumentNullException.ThrowIfNull(hyperedge);
        if (!_hyperedges.Remove(hyperedge))
        {
            return false;
        }

        foreach (var vertex in hyperedge.Vertices)
        {
            _incidence[vertex].Remove(hyperedge);
        }

        return true;
    }

    /// <summary>Removes every vertex and hyperedge.</summary>
    public void Clear()
    {
        _incidence.Clear();
        _hyperedges.Clear();
    }

    /// <summary>Determines whether the hypergraph contains <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to look up.</param>
    /// <returns><see langword="true"/> if the vertex is present.</returns>
    public bool ContainsVertex(TVertex vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _incidence.ContainsKey(vertex);
    }

    /// <summary>Gets the hyperedges incident to <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex whose hyperedges to enumerate.</param>
    /// <returns>The incident hyperedges.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the hypergraph.</exception>
    public IEnumerable<Hyperedge<TVertex>> IncidentHyperedges(TVertex vertex)
        => IncidenceOf(vertex);

    /// <summary>Gets the number of hyperedges incident to <paramref name="vertex"/>.</summary>
    /// <param name="vertex">The vertex to measure.</param>
    /// <returns>The vertex's degree.</returns>
    /// <exception cref="ArgumentException">The vertex is not in the hypergraph.</exception>
    public int Degree(TVertex vertex) => IncidenceOf(vertex).Count;

    /// <summary>
    /// Determines whether a chain of hyperedges connects
    /// <paramref name="first"/> and <paramref name="second"/>. Every vertex is
    /// connected to itself.
    /// </summary>
    /// <param name="first">One vertex.</param>
    /// <param name="second">The other vertex.</param>
    /// <returns><see langword="true"/> if both vertices share a connected component.</returns>
    /// <exception cref="ArgumentException">Either vertex is not in the hypergraph.</exception>
    public bool AreConnected(TVertex first, TVertex second)
    {
        IncidenceOf(first);
        IncidenceOf(second);

        if (VertexComparer.Equals(first, second))
        {
            return true;
        }

        var visited = new HashSet<TVertex>(VertexComparer) { first };
        var queue = new Queue<TVertex>();
        queue.Enqueue(first);

        while (queue.Count > 0)
        {
            foreach (var hyperedge in _incidence[queue.Dequeue()])
            {
                foreach (var member in hyperedge.Vertices)
                {
                    if (VertexComparer.Equals(member, second))
                    {
                        return true;
                    }

                    if (visited.Add(member))
                    {
                        queue.Enqueue(member);
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Computes the connected components: vertices grouped by reachability
    /// through shared hyperedges.
    /// </summary>
    /// <returns>One vertex set per component.</returns>
    public IReadOnlyList<IReadOnlySet<TVertex>> ConnectedComponents()
    {
        var visited = new HashSet<TVertex>(VertexComparer);
        var components = new List<IReadOnlySet<TVertex>>();

        foreach (var root in Vertices)
        {
            if (!visited.Add(root))
            {
                continue;
            }

            var component = new HashSet<TVertex>(VertexComparer) { root };
            var queue = new Queue<TVertex>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                foreach (var hyperedge in _incidence[queue.Dequeue()])
                {
                    foreach (var member in hyperedge.Vertices)
                    {
                        if (visited.Add(member))
                        {
                            component.Add(member);
                            queue.Enqueue(member);
                        }
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    private List<Hyperedge<TVertex>> IncidenceOf(
        TVertex vertex,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(vertex))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(vertex, paramName);
        return _incidence.TryGetValue(vertex, out var incident)
            ? incident
            : throw new ArgumentException($"Vertex '{vertex}' is not in the hypergraph.", paramName);
    }
}
