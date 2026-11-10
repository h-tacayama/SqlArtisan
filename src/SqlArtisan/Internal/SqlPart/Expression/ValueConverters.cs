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
        if (converted is BindValue bind and not BindArrayValue
            && ExpressionResolver.IsListed(bind.Value))
        {
            return bind;
        }

        if (converted is null || converted is BindValue || !ExpressionResolver.IsListed(converted))
        {
            throw new InvalidOperationException(
                $"The value converter for {type} returned "
                    + $"{converted?.GetType().ToString() ?? "null"}, which binds as no SQL value; "
                    + "return a type a value position binds.");
        }

        return new BindValue(converted);
    }
}
