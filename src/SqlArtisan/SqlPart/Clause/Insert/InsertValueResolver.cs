using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

internal static class InsertValueResolver
{
    internal static SqlExpression[] Resolve(object[] values)
    {
        var resolved = new SqlExpression[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            resolved[i] = Resolve(values[i]);
        }

        return resolved;
    }

    internal static SqlExpression Resolve(object value)
    {
        if (value is SqlExpression expr)
        {
            return expr;
        }
        else if (IsBindable(value))
        {
            return new BindValue(value);
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for InsertValue: {value.GetType()}");
        }
    }
}
