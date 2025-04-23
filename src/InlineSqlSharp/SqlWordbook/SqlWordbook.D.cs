using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static Distinct DISTINCT => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable DUAL => new();

    public static DecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        object @default) => new(
            Resolve(expr),
            Resolve(searchResultPairs),
            Resolve(@default));

    public static IDeleteBuilderDelete DELETE_FROM(AbstractTable table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DENSE_RANK() => new();
}
