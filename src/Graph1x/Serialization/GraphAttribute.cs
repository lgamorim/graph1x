using System.Globalization;
using System.Text.Json;

namespace Graph1x.Serialization;

/// <summary>The declared value type of a <see cref="GraphAttribute{T}"/>, mirroring GraphML's attr.type domain.</summary>
public enum GraphAttributeType
{
    /// <summary>A text value.</summary>
    String,

    /// <summary>A true/false value.</summary>
    Bool,

    /// <summary>A 32-bit integer value.</summary>
    Int,

    /// <summary>A 64-bit integer value.</summary>
    Long,

    /// <summary>A single-precision floating-point value.</summary>
    Float,

    /// <summary>A double-precision floating-point value.</summary>
    Double,
}

/// <summary>
/// A named, typed attribute read off a vertex or edge during export — the
/// same declaration drives GraphML (typed <c>&lt;key&gt;</c> elements) and
/// node-link JSON (typed properties). A <see langword="null"/> string value
/// omits the attribute on that element. Names must be unique per element
/// kind and must not collide with structural names (<c>id</c>,
/// <c>source</c>, <c>target</c>, or the <c>weight</c> key when a weight
/// selector is set).
/// </summary>
/// <typeparam name="T">The vertex or edge type the attribute reads from.</typeparam>
public sealed class GraphAttribute<T>
{
    private readonly Func<T, GraphAttributeValue> _value;

    private GraphAttribute(string name, GraphAttributeType type, Func<T, GraphAttributeValue> value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Type = type;
        _value = value;
    }

    /// <summary>Gets the attribute name, used as the GraphML key and the JSON property name.</summary>
    public string Name { get; }

    /// <summary>Gets the declared value type.</summary>
    public GraphAttributeType Type { get; }

    /// <summary>Declares a string attribute; a <see langword="null"/> value omits it on that element.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> String(string name, Func<T, string?> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.String, item => GraphAttributeValue.Of(value(item)));
    }

    /// <summary>Declares a boolean attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> Bool(string name, Func<T, bool> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.Bool, item => GraphAttributeValue.Of(value(item)));
    }

    /// <summary>Declares a 32-bit integer attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> Int(string name, Func<T, int> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.Int, item => GraphAttributeValue.Of(value(item)));
    }

    /// <summary>Declares a 64-bit integer attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> Long(string name, Func<T, long> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.Long, item => GraphAttributeValue.Of(value(item)));
    }

    /// <summary>Declares a single-precision floating-point attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> Float(string name, Func<T, float> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.Float, item => GraphAttributeValue.Of(value(item)));
    }

    /// <summary>Declares a double-precision floating-point attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">Reads the value off an element.</param>
    /// <returns>The attribute declaration.</returns>
    public static GraphAttribute<T> Double(string name, Func<T, double> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(name, GraphAttributeType.Double, item => GraphAttributeValue.Of(value(item)));
    }

    internal GraphAttributeValue GetValue(T item) => _value(item);
}

/// <summary>A typed attribute value: which kind it is and the matching payload.</summary>
internal readonly struct GraphAttributeValue
{
    private readonly GraphAttributeType _kind;
    private readonly string? _text;
    private readonly long _integer;
    private readonly double _number;
    private readonly bool _flag;

    private GraphAttributeValue(GraphAttributeType kind, string? text, long integer, double number, bool flag)
    {
        _kind = kind;
        _text = text;
        _integer = integer;
        _number = number;
        _flag = flag;
    }

    internal static GraphAttributeValue Of(string? value) => new(GraphAttributeType.String, value, 0, 0, false);

    internal static GraphAttributeValue Of(bool value) => new(GraphAttributeType.Bool, null, 0, 0, value);

    internal static GraphAttributeValue Of(int value) => new(GraphAttributeType.Int, null, value, 0, false);

    internal static GraphAttributeValue Of(long value) => new(GraphAttributeType.Long, null, value, 0, false);

    internal static GraphAttributeValue Of(float value) => new(GraphAttributeType.Float, null, 0, value, false);

    internal static GraphAttributeValue Of(double value) => new(GraphAttributeType.Double, null, 0, value, false);

    /// <summary>Renders the value as GraphML data text, or <see langword="null"/> to omit the element.</summary>
    internal string? ToGraphMlText() => _kind switch
    {
        GraphAttributeType.String => _text,
        GraphAttributeType.Bool => _flag ? "true" : "false",
        GraphAttributeType.Int or GraphAttributeType.Long => _integer.ToString(CultureInfo.InvariantCulture),
        GraphAttributeType.Float => ((float)_number).ToString(CultureInfo.InvariantCulture),
        _ => _number.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>Writes the value as a typed JSON property, or nothing when a string value is <see langword="null"/>.</summary>
    internal void WriteTo(Utf8JsonWriter writer, string name)
    {
        switch (_kind)
        {
            case GraphAttributeType.String when _text is not null:
                writer.WriteString(name, _text);
                break;
            case GraphAttributeType.String:
                break; // null: omit the property
            case GraphAttributeType.Bool:
                writer.WriteBoolean(name, _flag);
                break;
            case GraphAttributeType.Int or GraphAttributeType.Long:
                writer.WriteNumber(name, _integer);
                break;
            case GraphAttributeType.Float:
                writer.WriteNumber(name, (float)_number);
                break;
            default:
                writer.WriteNumber(name, _number);
                break;
        }
    }
}
