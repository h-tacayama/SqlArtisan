using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
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

    public static IDeleteBuilderDelete DeleteFrom(DbTableBase table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DenseRank() => new();
}
