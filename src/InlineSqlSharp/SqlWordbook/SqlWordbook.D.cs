using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DistinctKeyword Distinct => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();

    public static DecodeFunction Decode(
        object expr,
        (object search, object result)[] searchResultPairs,
        object @default) => new(
            Resolve(expr),
            Resolve(searchResultPairs),
            Resolve(@default));

    public static IDeleteBuilderDelete DeleteFrom(AbstractTable table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DenseRank() => new();
}
