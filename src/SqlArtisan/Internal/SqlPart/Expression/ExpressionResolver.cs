using System.Numerics;

namespace SqlArtisan.Internal;

internal static class ExpressionResolver
{
    internal const string NullValueMessage =
        "Value cannot be null. Use Sql.Null to represent SQL NULL.";

    internal static (SqlExpression, SqlExpression)[] Resolve((object, object)[] pairs)
    {
        if (pairs is null)
        {
            throw new ArgumentNullException(nameof(pairs), NullValueMessage);
        }

        var resolved = new (SqlExpression, SqlExpression)[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            resolved[i] = (Resolve(pairs[i].Item1), Resolve(pairs[i].Item2));
        }

        return resolved;
    }

    internal static SqlExpression[] Resolve(object[] items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items), NullValueMessage);
        }

        var resolved = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = Resolve(items[i]);
        }

        return resolved;
    }

    internal static SqlExpression Resolve(object item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item), NullValueMessage);
        }

#pragma warning disable IDE0046
        if (item is SqlExpression expr)
        {
            return expr;
        }
        else if (IsBindable(item))
        {
            return new BindValue(item);
        }
        else
        {
            throw UnresolvableValue("SqlExpression", item);
        }
#pragma warning restore IDE0046
    }

    // Builds the exception for a value that reached a value position but isn't a
    // usable expression, shared by every resolver. A "pending" type (a window
    // function before .Over(...), an ordered-set aggregate before .WithinGroup(...))
    // gets an actionable completion hint; any other unsupported type gets the
    // generic message, where `position` names the position the value reached
    // (e.g. "SelectItem", "GroupByItem").
    internal static ArgumentException UnresolvableValue(string position, object item) =>
        item is IIncompleteExpression incomplete
            ? new ArgumentException(
                $"{item.GetType().Name} is not a complete SQL expression. {incomplete.CompletionHint}")
            : new ArgumentException(
                $"Invalid type for {position}: {item.GetType()}");

    internal static bool IsBindable(object value) =>
        IsBoolean(value)
        || IsCharacter(value)
        || IsDateTime(value)
        || IsNumeric(value)
        || IsEnum(value);

    internal static bool IsBoolean(object value) =>
        value is bool;

    internal static bool IsCharacter(object value) =>
        value is char
        || value is string;

    internal static bool IsDateTime(object value) =>
        value is DateTime
        || value is DateOnly
        || value is TimeOnly;

    internal static bool IsNumeric(object value) =>
        value is sbyte
        || value is byte
        || value is short
        || value is ushort
        || value is int
        || value is uint
        || value is nint
        || value is nuint
        || value is long
        || value is ulong
        || value is float
        || value is double
        || value is decimal
        || value is Complex;

    internal static bool IsEnum(object value) =>
        value is Enum;
}
