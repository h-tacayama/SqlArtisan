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

    internal static SqlExpression[] Resolve<T>(IReadOnlyCollection<T> items)
    {
        var resolved = new SqlExpression[items.Count];

        int i = 0;
        foreach (T item in items)
        {
            resolved[i++] = Resolve(item!);
        }

        return resolved;
    }

    // A T[]-typed sibling of the IReadOnlyCollection<T> overload: foreach over a
    // concrete array is a compiler-recognized zero-allocation loop, while the same
    // foreach through the interface allocates the array's enumerator (measured
    // 32 B/call, ADR 0006). Every other array foreach in the library is this shape.
    internal static SqlExpression[] Resolve<T>(T[] items)
    {
        var resolved = new SqlExpression[items.Length];

        int i = 0;
        foreach (T item in items)
        {
            resolved[i++] = Resolve(item!);
        }

        return resolved;
    }

    // A variadic function's arguments resolved into one array. Resolving the
    // params tail on its own and then spread-merging it into the leading
    // arguments allocates a second array per construction (ADR 0006). Each
    // overload resolves its leading arguments first, so a null there still
    // reports before a null tail does.
    internal static SqlExpression[] ResolveVariadic(object first, object[] items)
    {
        SqlExpression resolvedFirst = Resolve(first);
        SqlExpression[] resolved = NewVariadicArray(items, 1);

        resolved[0] = resolvedFirst;
        ResolveInto(items, resolved, 1);
        return resolved;
    }

    internal static SqlExpression[] ResolveVariadic(object first, object second, object[] items)
    {
        SqlExpression resolvedFirst = Resolve(first);
        SqlExpression resolvedSecond = Resolve(second);
        SqlExpression[] resolved = NewVariadicArray(items, 2);

        resolved[0] = resolvedFirst;
        resolved[1] = resolvedSecond;
        ResolveInto(items, resolved, 2);
        return resolved;
    }

    internal static SqlExpression[] ResolveVariadic(
        object first,
        object second,
        object third,
        object[] items)
    {
        SqlExpression resolvedFirst = Resolve(first);
        SqlExpression resolvedSecond = Resolve(second);
        SqlExpression resolvedThird = Resolve(third);
        SqlExpression[] resolved = NewVariadicArray(items, 3);

        resolved[0] = resolvedFirst;
        resolved[1] = resolvedSecond;
        resolved[2] = resolvedThird;
        ResolveInto(items, resolved, 3);
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
        else if (item is ISubquery subquery)
        {
            return new ScalarSubquery(subquery);
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

    // The tail parameter is named `items` here and above so a null tail keeps the
    // message every other array-taking resolver entry point produces.
    private static SqlExpression[] NewVariadicArray(object[] items, int leadingCount)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items), NullValueMessage);
        }

        return new SqlExpression[items.Length + leadingCount];
    }

    private static void ResolveInto(object[] items, SqlExpression[] target, int offset)
    {
        for (int i = 0; i < items.Length; i++)
        {
            target[i + offset] = Resolve(items[i]);
        }
    }

    internal static bool IsBindable(object value) =>
        IsBoolean(value)
        || IsCharacter(value)
        || IsDateTime(value)
        || IsNumeric(value)
        || IsEnum(value);

    // Type-level twin of IsBindable for BindArray's element check: the element
    // type is fixed at the call site, so even an empty array validates
    // deterministically. Keep the two sets identical.
    internal static bool IsBindableType(Type type)
    {
        Type unwrapped = Nullable.GetUnderlyingType(type) ?? type;
        return unwrapped == typeof(bool)
            || unwrapped == typeof(char)
            || unwrapped == typeof(string)
            || unwrapped == typeof(DateTime)
            || unwrapped == typeof(DateOnly)
            || unwrapped == typeof(TimeOnly)
            || unwrapped == typeof(sbyte)
            || unwrapped == typeof(byte)
            || unwrapped == typeof(short)
            || unwrapped == typeof(ushort)
            || unwrapped == typeof(int)
            || unwrapped == typeof(uint)
            || unwrapped == typeof(nint)
            || unwrapped == typeof(nuint)
            || unwrapped == typeof(long)
            || unwrapped == typeof(ulong)
            || unwrapped == typeof(float)
            || unwrapped == typeof(double)
            || unwrapped == typeof(decimal)
            || unwrapped == typeof(Complex)
            || unwrapped.IsEnum;
    }

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
