using System.Collections.Concurrent;

namespace SqlArtisan.Internal;

// The converters SqlArtisanConfig.AddValueConverter registers, keyed by the value's exact
// type (#810).
internal static class ValueConverters
{
    private static readonly ConcurrentDictionary<Type, Func<object, object?>> s_converters =
        new();

    internal static void Add(Type type, Func<object, object?> convert) =>
        s_converters[type] = convert;

    internal static void Clear() => s_converters.Clear();

    internal static bool Has(Type type) => s_converters.ContainsKey(type);

    // params object[] takes an array of a reference element type as the list itself, so a
    // registered one could be the list or one value; In, NotIn and a list of rows declare
    // the list reading in their types and skip this.
    internal static void ThrowIfListIsRegisteredValue(object?[]? items)
    {
        Type? type = items?.GetType();
        if (type is not null && type != typeof(object[]) && s_converters.ContainsKey(type))
        {
            throw new ArgumentException(
                $"A {type} passed as the whole argument list could be the list or one value, "
                    + "since a value converter is registered for it; pass (object)value to bind "
                    + "it as one value, or [.. value] to pass its elements.");
        }
    }

    // Converted when the value is passed, so every execution path receives it (#811).
    internal static BindValue Convert(object value)
    {
        Type type = value.GetType();
        if (!s_converters.TryGetValue(type, out Func<object, object?>? convert))
        {
            throw ExpressionResolver.UnresolvableValue("SqlExpression", value);
        }

        object? converted = convert(value);

        // A BindValue is judged and named by what it carries, the part the remedy is about.
        BindValue? carrier = converted as BindValue;
        object? payload = carrier is null ? converted : carrier.Value;
        if (payload is null or DBNull && carrier is not BindArrayValue)
        {
            return new ConvertedNull(type, carrier?.DbType, carrier?.Size);
        }

        if (carrier is BindArrayValue || !IsConvertedValueBindable(payload!))
        {
            throw new InvalidOperationException(
                $"The value converter for {type} produced {payload!.GetType()}; a converter, or "
                    + "a type handler's SetValue, must produce a type SqlArtisan binds directly, "
                    + "a byte[], null or a BindValue carrying one, and a provider's own type "
                    + "binds at the call through new BindValue(value).");
        }

        return carrier ?? new BindValue(payload!);
    }

    // byte[] stays off the list only because In(...) reads an array per element (#669);
    // a converted value is one value, so binary data binds as it does through BindValue.
    private static bool IsConvertedValueBindable(object value) =>
        ExpressionResolver.IsDirectlyBindable(value) || value is byte[];
}
