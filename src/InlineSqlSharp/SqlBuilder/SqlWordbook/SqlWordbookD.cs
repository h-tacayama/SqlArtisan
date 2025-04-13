using System.Diagnostics;
using System.Numerics;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static AllOrDistinct DISTINCT => AllOrDistinct.Distinct;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable DUAL => new();

    public static CharacterDecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        CharacterExpr @default) =>
        new(expr, searchResultPairs, @default);

    public static CharacterDecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        char @default) =>
        new(expr, searchResultPairs, @default);

    public static CharacterDecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        string @default) =>
        new(expr, searchResultPairs, @default);

    public static DateTimeDecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        DateTimeExpr @default) =>
        new(expr, searchResultPairs, @default);

    public static DateTimeDecodeFunction DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        DateTime @default) =>
        new(expr, searchResultPairs, @default);

    public static NumericDecodeFunction<decimal> DECODE(
        object expr,
        (object search, object result)[] searchResultPairs,
        NumericExpr @default) =>
        new(expr, searchResultPairs, @default);

    public static NumericDecodeFunction<TDefault> DECODE<TDefault>(
        object expr,
        (object search, object result)[] searchResultPairs,
        INumber<TDefault> @default) where TDefault : INumber<TDefault> =>
        new(expr, searchResultPairs, @default);

    public static IDeleteBuilderDelete DELETE_FROM(AbstractTable table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DENSE_RANK() => new();
}
