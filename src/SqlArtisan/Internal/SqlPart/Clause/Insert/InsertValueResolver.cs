using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class InsertValueResolver
{
    internal static SqlExpression[] Resolve(object?[] values)
    {
        var resolved = new SqlExpression[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            resolved[i] = Resolve(values[i]);
        }

        return resolved;
    }

    internal static SqlExpression Resolve(object? value)
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
        else if (IsBindable(value))
        {
            return new BindValue(value);
        }
        else
        {
            throw UnresolvableValue("InsertValue", value);
        }
    }
}
