using System.Text.Json;
using Graph1x.Edges;

namespace Graph1x.Serialization;

/// <summary>
/// Imports node-link JSON documents produced by
/// <see cref="GraphJsonExtensions.ToJson{TVertex, TEdge}(IReadOnlyGraph{TVertex, TEdge})"/>
/// or by other tools using the same shape. Vertices are strings; graphs
/// import into multigraph types, the lossless superset. Unknown properties
/// are ignored for forward compatibility; structural problems raise
/// <see cref="FormatException"/>.
/// </summary>
public static class GraphJson
{
    /// <summary>Parses a node-link JSON document, choosing directedness from its <c>directed</c> property.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>A directed or undirected multigraph over string node ids.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON.</exception>
    public static IMutableGraph<string, Edge<string>> Parse(string json)
    {
        using var document = LoadDocument(json);
        var root = RootObject(document);
        return IsDirected(root)
            ? Populate(new DirectedMultigraph<string, Edge<string>>(), root, MakeEdge)
            : Populate(new UndirectedMultigraph<string, Edge<string>>(), root, MakeEdge);

        static Edge<string> MakeEdge(string source, string target, JsonElement _) => new(source, target);
    }

    /// <summary>Parses a node-link JSON document that must declare <c>"directed": true</c>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The imported directed multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON or is undirected.</exception>
    public static DirectedMultigraph<string, Edge<string>> ParseDirected(string json)
    {
        using var document = LoadDocument(json);
        var root = RequireDirection(document, directed: true);
        return Populate(new DirectedMultigraph<string, Edge<string>>(), root, (s, t, _) => new Edge<string>(s, t));
    }

    /// <summary>Parses a node-link JSON document that must declare <c>"directed": false</c>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The imported undirected multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON or is directed.</exception>
    public static UndirectedMultigraph<string, Edge<string>> ParseUndirected(string json)
    {
        using var document = LoadDocument(json);
        var root = RequireDirection(document, directed: false);
        return Populate(new UndirectedMultigraph<string, Edge<string>>(), root, (s, t, _) => new Edge<string>(s, t));
    }

    /// <summary>Parses a directed node-link document, reading edge weights from <c>weight</c> properties (0 when absent).</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The imported weighted directed multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON, is undirected, or holds a non-numeric weight.</exception>
    public static DirectedMultigraph<string, WeightedEdge<string, double>> ParseDirectedWeighted(string json)
    {
        using var document = LoadDocument(json);
        var root = RequireDirection(document, directed: true);
        return Populate(
            new DirectedMultigraph<string, WeightedEdge<string, double>>(),
            root,
            (s, t, element) => new WeightedEdge<string, double>(s, t, ReadWeight(element)));
    }

    /// <summary>Parses an undirected node-link document, reading edge weights from <c>weight</c> properties (0 when absent).</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The imported weighted undirected multigraph.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON, is directed, or holds a non-numeric weight.</exception>
    public static UndirectedMultigraph<string, WeightedEdge<string, double>> ParseUndirectedWeighted(string json)
    {
        using var document = LoadDocument(json);
        var root = RequireDirection(document, directed: false);
        return Populate(
            new UndirectedMultigraph<string, WeightedEdge<string, double>>(),
            root,
            (s, t, element) => new WeightedEdge<string, double>(s, t, ReadWeight(element)));
    }

    /// <summary>
    /// Parses a node-link JSON document into structure plus attributes:
    /// every non-structural scalar property becomes an attribute
    /// (<see cref="string"/>, <see cref="double"/>, or <see cref="bool"/>);
    /// nulls, arrays, and nested objects are ignored. Directedness follows
    /// the <c>directed</c> property.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The parsed document: graph, vertex data, and edge data.</returns>
    /// <exception cref="FormatException">The document is not well-formed node-link JSON.</exception>
    public static GraphDocument ParseDocument(string json)
    {
        using var document = LoadDocument(json);
        var root = RootObject(document);

        IMutableGraph<string, Edge<string>> graph = IsDirected(root)
            ? new DirectedMultigraph<string, Edge<string>>()
            : new UndirectedMultigraph<string, Edge<string>>();
        var vertexData = new Dictionary<string, IReadOnlyDictionary<string, object>>();
        var edgeData = new List<IReadOnlyDictionary<string, object>>();

        if (root.TryGetProperty("nodes", out var nodes))
        {
            RequireArray(nodes, "nodes");
            foreach (var node in nodes.EnumerateArray())
            {
                var id = RequiredString(node, "id", "node");
                graph.AddVertex(id);
                vertexData[id] = ReadScalarProperties(node, "id");
            }
        }

        if (root.TryGetProperty("edges", out var edges))
        {
            RequireArray(edges, "edges");
            foreach (var edge in edges.EnumerateArray())
            {
                var source = RequiredString(edge, "source", "edge");
                var target = RequiredString(edge, "target", "edge");
                graph.AddEdge(new Edge<string>(source, target));
                edgeData.Add(ReadScalarProperties(edge, "source", "target"));
            }
        }

        return new GraphDocument(graph, vertexData, edgeData);
    }

    private static IReadOnlyDictionary<string, object> ReadScalarProperties(
        JsonElement element,
        params string[] structuralNames)
    {
        var data = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
        {
            if (structuralNames.Contains(property.Name))
            {
                continue;
            }

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    data[property.Name] = property.Value.GetString()!;
                    break;
                case JsonValueKind.Number:
                    data[property.Name] = property.Value.GetDouble();
                    break;
                case JsonValueKind.True or JsonValueKind.False:
                    data[property.Name] = property.Value.GetBoolean();
                    break;
                default:
                    break; // nulls, arrays, and objects are ignored for forward compatibility
            }
        }

        return data;
    }

    private static JsonDocument LoadDocument(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new FormatException("The document is not well-formed JSON.", exception);
        }
    }

    private static JsonElement RootObject(JsonDocument document)
        => document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement
            : throw new FormatException("The document root must be a JSON object.");

    private static bool IsDirected(JsonElement root)
        => root.TryGetProperty("directed", out var directed)
            && directed.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? directed.GetBoolean()
            : throw new FormatException("The document is missing the required boolean 'directed' property.");

    private static JsonElement RequireDirection(JsonDocument document, bool directed)
    {
        var root = RootObject(document);
        var actual = IsDirected(root);
        if (actual != directed)
        {
            throw new FormatException(
                $"The document declares \"directed\": {(actual ? "true" : "false")}, "
                + $"but {(directed ? "a directed" : "an undirected")} graph was requested.");
        }

        return root;
    }

    private static TGraph Populate<TGraph, TEdge>(
        TGraph graph,
        JsonElement root,
        Func<string, string, JsonElement, TEdge> edgeFactory)
        where TGraph : IMutableGraph<string, TEdge>
        where TEdge : IEdge<string>
    {
        if (root.TryGetProperty("nodes", out var nodes))
        {
            RequireArray(nodes, "nodes");
            foreach (var node in nodes.EnumerateArray())
            {
                graph.AddVertex(RequiredString(node, "id", "node"));
            }
        }

        if (root.TryGetProperty("edges", out var edges))
        {
            RequireArray(edges, "edges");
            foreach (var edge in edges.EnumerateArray())
            {
                var source = RequiredString(edge, "source", "edge");
                var target = RequiredString(edge, "target", "edge");
                graph.AddEdge(edgeFactory(source, target, edge));
            }
        }

        return graph;
    }

    private static void RequireArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException($"The '{name}' property must be a JSON array.");
        }
    }

    private static string RequiredString(JsonElement element, string name, string context)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()!
            : throw new FormatException($"A {context} object is missing the required string '{name}' property.");

    private static double ReadWeight(JsonElement edge)
    {
        if (!edge.TryGetProperty("weight", out var weight))
        {
            return 0.0;
        }

        return weight.ValueKind == JsonValueKind.Number
            ? weight.GetDouble()
            : throw new FormatException("An edge 'weight' property must be a JSON number.");
    }
}
