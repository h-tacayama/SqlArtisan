using System.Numerics;

namespace SqlArtisan;

internal static class ExprResolver
{
    internal static (AbstractExpr, AbstractExpr)[] Resolve((object, object)[] pairs)
    {
        var resolved = new (AbstractExpr, AbstractExpr)[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            resolved[i] = (Resolve(pairs[i].Item1), Resolve(pairs[i].Item2));
        }

        return resolved;
    }

    internal static AbstractExpr[] Resolve(object[] items)
    {
        var resolved = new AbstractExpr[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = Resolve(items[i]);
        }

        return resolved;
    }

    internal static AbstractExpr Resolve(object item)
    {
        if (item is AbstractExpr expr)
        {
            return expr;
        }
        else if (IsBindable(item))
        {
            return new BindValue(item);
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for Expr: {item.GetType()}");
        }
    }

    internal static bool IsBindable(object value) =>
        IsCharacter(value)
        || IsDateTime(value)
        || IsNumeric(value)
        || IsEnum(value);

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
