using System.Collections.Concurrent;

namespace SqlArtisan.Internal;

// The converters SqlArtisanConfig.AddValueConverter registers, keyed by the value's exact
// type. A listed type never reaches the lookup (ExpressionResolver checks the list first),
// so a build binding only listed types pays nothing for the registry (#810).
internal static class ValueConverters
{
    private static readonly ConcurrentDictionary<Type, Func<object, object?>> s_converters =
        new();

    internal static void Add(Type type, Func<object, object?> convert) =>
        s_converters[type] = convert;

    internal static void Clear() => s_converters.Clear();

    internal static bool Has(Type type) => s_converters.ContainsKey(type);

    // Converted when the value is passed, so every execution path, raw ADO.NET and
    // ArrayBind included, receives the underlying value: a provider handed the object
    // itself may bind its ToString() text (#811).
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

        if (carrier is BindArrayValue || !Bindable(payload!))
        {
            throw new InvalidOperationException(
                $"The value converter for {type} returned {payload!.GetType()}; return a listed "
                    + "type, a byte[], null or a BindValue carrying one, or bind a value the "
                    + "provider takes as it is at the call with new BindValue(value).");
        }

        return carrier ?? new BindValue(payload!);
    }

    // byte[] stays off the list only because In(...) reads an array per element (#669);
    // a converted value is one value, so binary data binds as it does through BindValue.
    private static bool Bindable(object value) =>
        ExpressionResolver.IsListed(value) || value is byte[];
}
