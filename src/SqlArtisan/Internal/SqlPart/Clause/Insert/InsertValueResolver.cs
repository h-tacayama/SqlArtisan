using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class InsertValueResolver
{
    internal const string NullRowMessage =
        "A VALUES row cannot be a null array; pass Sql.Null for a NULL value.";

    // params object[] takes an object[] row as the row itself, so an array among its
    // values is several rows, a row of another element type, or an array value (#679).
    internal static void ThrowIfArrayValue(object?[] values, string? severalRows)
    {
        foreach (object? value in values)
        {
            if (value is Array)
            {
                throw new ArgumentException(
                    $"A VALUES value cannot be an array ({value.GetType()}); bind an array "
                        + "value with BindArray(...), and pass "
                        + (severalRows is null
                            ? "the row as object[]."
                            : $"a row as object[], and several as {severalRows}."));
            }
        }
    }

    internal static SqlExpression[] Resolve(object?[] values) => Resolve(values, "InsertValue");

    internal static SqlExpression[] Resolve(object?[] values, string position)
    {
        CollectionGuard.ThrowIfEmpty(
            values, nameof(values), "A VALUES row requires at least one value.");

        var resolved = new SqlExpression[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            resolved[i] = Resolve(values[i], position);
        }

        return resolved;
    }

    internal static SqlExpression Resolve(object? value) => Resolve(value, "InsertValue");

    internal static SqlExpression Resolve(object? value, string position)
    {
        if (value is null)
        {
            // A null value inserts a SQL NULL (the NULL keyword, not a bind
            // parameter): valid on every dialect and unambiguous. (#169)
            return new NullExpression();
        }
        else if (value is SqlExpression expr)
        {
            return expr;
        }
        else if (value is ISubquery subquery)
        {
            return new ScalarSubquery(subquery);
        }
        else if (IsBindable(value))
        {
            return BindImplicitly(value);
        }
        else
        {
            throw UnresolvableValue(position, value);
        }
    }
}
