using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static Null NULL => new();

    public static NotCondition NOT(AbstractCondition condition) => new(condition);

    public static NotExistsCondition NOT_EXISTS(ISubquery subquery) =>
        new(subquery);

    public static NvlFunction NVL(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));
}
