using System.Numerics;

namespace SqlArtisan.Internal;

internal static class ExpressionResolver
{
    internal const string NullValueMessage = "A C# null is not SQL NULL; pass Sql.Null instead.";

    internal const string NullSimpleCaseMessage =
        "A simple CASE cannot match a C# null; test for NULL with a searched When(expr.IsNull).";

    // Every entry point takes the caller's parameter name, so a null argument or element
    // reports the public member's own parameter rather than this class's.
    internal static (SqlExpression, SqlExpression)[] Resolve(
        (object, object)[] pairs,
        string paramName)
    {
        if (pairs is null)
        {
            throw new ArgumentNullException(paramName, NullValueMessage);
        }

        var resolved = new (SqlExpression, SqlExpression)[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            resolved[i] = Resolve(pairs[i], paramName);
        }

        return resolved;
    }

    internal static (SqlExpression, SqlExpression) Resolve(
        (object, object) pair,
        string paramName) =>
        (Resolve(pair.Item1, paramName), Resolve(pair.Item2, paramName));

    internal static SqlExpression[] Resolve(object[] items, string paramName)
    {
        if (items is null)
        {
            throw new ArgumentNullException(paramName, NullValueMessage);
        }

        var resolved = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = Resolve(items[i], paramName);
        }

        return resolved;
    }

    // A variadic function's arguments resolved into one array. Resolving the
    // params tail on its own and then spread-merging it into the leading
    // arguments allocates a second array per construction (ADR 0006). Each
    // overload resolves its leading arguments first, so a null there still
    // reports before a null tail does.
    internal static SqlExpression[] ResolveVariadic(
        object first,
        string firstName,
        object[] items,
        string itemsName)
    {
        SqlExpression resolvedFirst = Resolve(first, firstName);
        SqlExpression[] resolved = NewVariadicArray(items, itemsName, 1);

        resolved[0] = resolvedFirst;
        ResolveInto(items, itemsName, resolved, 1);
        return resolved;
    }

    internal static SqlExpression[] ResolveVariadic(
        object first,
        string firstName,
        object second,
        string secondName,
        object[] items,
        string itemsName)
    {
        SqlExpression resolvedFirst = Resolve(first, firstName);
        SqlExpression resolvedSecond = Resolve(second, secondName);
        SqlExpression[] resolved = NewVariadicArray(items, itemsName, 2);

        resolved[0] = resolvedFirst;
        resolved[1] = resolvedSecond;
        ResolveInto(items, itemsName, resolved, 2);
        return resolved;
    }

    internal static SqlExpression[] ResolveVariadic(
        object first,
        string firstName,
        object second,
        string secondName,
        object third,
        string thirdName,
        object[] items,
        string itemsName)
    {
        SqlExpression resolvedFirst = Resolve(first, firstName);
        SqlExpression resolvedSecond = Resolve(second, secondName);
        SqlExpression resolvedThird = Resolve(third, thirdName);
        SqlExpression[] resolved = NewVariadicArray(items, itemsName, 3);

        resolved[0] = resolvedFirst;
        resolved[1] = resolvedSecond;
        resolved[2] = resolvedThird;
        ResolveInto(items, itemsName, resolved, 3);
        return resolved;
    }

    internal static SqlExpression Resolve(object item, string paramName)
    {
        if (item is null)
        {
            throw new ArgumentNullException(paramName, NullValueMessage);
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
            return BindImplicitly(item);
        }
        else
        {
            throw UnresolvableValue("SqlExpression", item);
        }
#pragma warning restore IDE0046
    }

    // A grouping, partition or DISTINCT ON key: a bound value there is a constant
    // that groups nothing (#673), so only an expression or a subquery resolves.
    internal static SqlExpression[] ResolveKeys(
        object[] items,
        string paramName,
        string position,
        string construct)
    {
        if (items is null)
        {
            throw NullKey(paramName, construct);
        }

        var resolved = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = ResolveKey(items[i], paramName, position, construct);
        }

        return resolved;
    }

    internal static SqlExpression ResolveKey(
        object item,
        string paramName,
        string position,
        string construct)
    {
        if (item is null)
        {
            throw NullKey(paramName, construct);
        }

        return item switch
        {
            SqlExpression expr => expr,
            ISubquery subquery => new ScalarSubquery(subquery),
            _ => throw UnresolvableValue(position, item),
        };
    }

    // NullValueMessage's remedy, Sql.Null, is a constant key here: silent where an engine
    // runs it, an error where one rejects it, so no remedy in either case.
    internal static ArgumentNullException NullKey(string paramName, string construct)
    {
        string article = "AEIOU".Contains(construct[0], StringComparison.Ordinal) ? "An" : "A";
        return new(
            paramName,
            $"{article} {construct} key cannot be a C# null; pass a column or an expression.");
    }

    // The comparison and predicate positions' resolver. A C# null there is usually a
    // nullable variable meant as a NULL test, and NullValueMessage's Sql.Null would build
    // `= NULL`, which is unknown rather than a test; paramName is the caller's own.
    internal static SqlExpression ResolveCompared(object item, string paramName, string construct)
    {
        if (item is null)
        {
            // `=`'s right side is also the SET assignment, where Sql.Null is right.
            throw construct == Operators.Equal
                ? new ArgumentNullException(paramName, "= cannot take a C# null; "
                    + "test for NULL with .IsNull, or write Sql.Null to assign NULL.")
                : NullCompared(paramName, construct);
        }

        return Resolve(item, paramName);
    }

    // The right operand an ANY / ALL / SOME completes, in the slots QuantifiedExpression's
    // remarks list; every other position leaves it to the IIncompleteExpression message.
    internal static SqlExpression ResolveQuantifiable(
        object item,
        string paramName,
        string construct) =>
        item is QuantifiedExpression or QuantifiedSubquery
            ? new PositionBoundOperand((SqlPart)item)
            : ResolveCompared(item, paramName, construct);

    // The right operand of + / -, the arithmetic slot MySQL's INTERVAL completes (#666).
    internal static SqlExpression ResolveIntervalOperand(object item, string paramName) =>
        item is IntervalExpression interval
            ? new PositionBoundOperand(interval)
            : Resolve(item, paramName);

    internal static SqlExpression[] ResolveCompared(
        object[] items,
        string paramName,
        string construct)
    {
        var resolved = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = ResolveCompared(items[i], paramName, construct);
        }

        return resolved;
    }

    internal static SqlExpression[] ResolveCompared<T>(
        IReadOnlyCollection<T> items,
        string paramName,
        string construct)
    {
        var resolved = new SqlExpression[items.Count];

        int i = 0;
        foreach (T item in items)
        {
            resolved[i++] = ResolveCompared(item!, paramName, construct);
        }

        return resolved;
    }

    internal static SqlExpression[] ResolveCompared<T>(
        T[] items,
        string paramName,
        string construct)
    {
        var resolved = new SqlExpression[items.Length];

        int i = 0;
        foreach (T item in items)
        {
            resolved[i++] = ResolveCompared(item!, paramName, construct);
        }

        return resolved;
    }

    // A simple CASE compares its operand with each WHEN value by `=`, so a null
    // either side never matches and NullValueMessage's Sql.Null would not either.
    internal static SqlExpression ResolveSimpleCase(object item, string paramName)
    {
        if (item is null)
        {
            throw new ArgumentNullException(paramName, NullSimpleCaseMessage);
        }

        return Resolve(item, paramName);
    }

    internal static ArgumentNullException NullCompared(string paramName, string construct) =>
        new(paramName, $"{construct} cannot compare a C# null; "
            + "test for NULL with .IsNull or .IsNotNull.");

    // Builds the exception for a value that reached a value position but isn't a
    // usable expression, shared by every resolver. A "pending" type, a SELECT prefix and
    // a grouping set get a remedy; any other type the generic message, naming the position.
    internal static ArgumentException UnresolvableValue(string position, object item) =>
        item switch
        {
            IIncompleteExpression incomplete => new(incomplete.IncompleteMessage),
            DistinctKeyword or DistinctOnKeyword or SqlHints or TopClause
                => new(MisplacedPrefixMessage(item, position)),
            GroupingSet => new(
                "A grouping set built with Group(...) goes inside GroupingSets(...), "
                    + "Rollup(...) or Cube(...)."),
            _ => new($"Invalid type for {position}: {item.GetType()}"),
        };

    // A SELECT prefix is a part as an item is, so one written out of the order the
    // overloads take lands in an item slot; the message names that order (#679).
    private static string MisplacedPrefixMessage(object prefix, string position)
    {
        string keyword = prefix switch
        {
            DistinctKeyword => "DISTINCT",
            DistinctOnKeyword => "DISTINCT ON",
            TopClause => "TOP",
            _ => "An optimizer hint",
        };

        if (position == "SelectItem")
        {
            return $"{keyword} goes ahead of the select items, in an order a Select(...) "
                + "overload takes: hints, then DISTINCT or DISTINCT ON, or DISTINCT, then TOP.";
        }

        return prefix is DistinctKeyword
            ? "DISTINCT is no value; Select(Distinct, ...) and an aggregate such as "
                + "Count(Distinct, x) take it first."
            : $"{keyword} is no value; Select(...) takes it ahead of its items.";
    }

    private static SqlExpression[] NewVariadicArray(
        object[] items,
        string itemsName,
        int leadingCount)
    {
        if (items is null)
        {
            throw new ArgumentNullException(itemsName, NullValueMessage);
        }

        return new SqlExpression[items.Length + leadingCount];
    }

    private static void ResolveInto(
        object[] items,
        string itemsName,
        SqlExpression[] target,
        int offset)
    {
        for (int i = 0; i < items.Length; i++)
        {
            target[i + offset] = Resolve(items[i], itemsName);
        }
    }

    // A function-argument enum in a value slot was passed at an arity with no slot of
    // its type and would bind as the value (#667). Sql.Bind and new BindValue(...) skip
    // this, so binding one stays writable; no factory takes Dbms or DbTypeCategory.
    internal static BindValue BindImplicitly(object value) =>
        value is DateTimePart or RegexpOptions or SearchModifier
            ? throw new ArgumentException($"{value.GetType().Name} is a function argument, not a "
                + $"value; pass it to a {value.GetType().Name} parameter, or, to store or compare "
                + "the enum itself, bind it with Sql.Bind(...).")
            : new BindValue(value);

    internal static bool IsBindable(object value) =>
        IsBoolean(value)
        || IsCharacter(value)
        || IsDateTime(value)
        || IsNumeric(value)
        || IsEnum(value)
        || value is Guid;

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
            || unwrapped == typeof(DateTimeOffset)
            || unwrapped == typeof(DateOnly)
            || unwrapped == typeof(TimeOnly)
            || unwrapped == typeof(TimeSpan)
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
            || unwrapped.IsEnum
            || unwrapped == typeof(Guid);
    }

    internal static bool IsBoolean(object value) =>
        value is bool;

    internal static bool IsCharacter(object value) =>
        value is char
        || value is string;

    internal static bool IsDateTime(object value) =>
        value is DateTime
        || value is DateTimeOffset
        || value is DateOnly
        || value is TimeOnly
        || value is TimeSpan;

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
