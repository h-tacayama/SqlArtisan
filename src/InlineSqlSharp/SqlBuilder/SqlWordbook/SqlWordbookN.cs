using System.Diagnostics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CharacterNull NULL_AS_CHARACTER => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DateTimeNull NULL_AS_DATETIME => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NumericNull NULL_AS_NUMERIC => new();

    public static NotCondition NOT(ICondition condition) => new(condition);

    public static NotExistsCondition NOT_EXISTS(ISubquery subquery) =>
        new(subquery);

    public static CharacterNvlFunction NVL(
        CharacterExpr expr1,
        CharacterExpr expr2) => new(expr1, expr2);

    public static DateTimeNvlFunction NVL(
        DateTimeExpr expr1,
        DateTimeExpr expr2) => new(expr1, expr2);

    public static NumericNvlFunction NVL(
        NumericExpr expr1,
        NumericExpr expr2) => new(expr1, expr2);
}
