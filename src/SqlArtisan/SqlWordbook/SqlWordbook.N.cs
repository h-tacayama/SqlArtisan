using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NullExpr Null => new();

    public static NotCondition Not(AbstractCondition condition) => new(condition);

    public static NotExistsCondition NotExists(ISubquery subquery) =>
        new(subquery);

    public static NvlFunction Nvl(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));
}
