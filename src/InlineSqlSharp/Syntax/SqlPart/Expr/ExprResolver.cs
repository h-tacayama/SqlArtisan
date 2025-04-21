using System.Numerics;

namespace InlineSqlSharp;

internal static class ExprResolver
{
    internal static (AbstractExpr, AbstractExpr)[] Resolve(
        (object, object)[] pairs) =>
        pairs.Select(pair =>
        (Resolve(pair.Item1),
        Resolve(pair.Item2))).ToArray();

    internal static AbstractExpr[] Resolve(object[] items) =>
        items.Select(Resolve).ToArray();

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
