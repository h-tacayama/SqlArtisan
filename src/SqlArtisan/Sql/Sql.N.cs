using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NullExpression Null => new();

    public static NextValFunction NextVal(string sequenceName) =>
        new(sequenceName);

    public static NotCondition Not(SqlCondition condition) => new(condition);

    public static NotExistsCondition NotExists(ISubquery subquery) =>
        new(subquery);

    public static NvlFunction Nvl(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));
}
