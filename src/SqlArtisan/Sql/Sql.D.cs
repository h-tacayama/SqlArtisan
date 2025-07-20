using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static DatepartFunction Datepart(Datepart datepart, object source) =>
        new(datepart, Resolve(source));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result)[] searchResultPairs,
        object @default) => new(
            Resolve(expr),
            Resolve(searchResultPairs),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] { searchResultPair }),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        (object search, object result) searchResultPair9,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,
                searchResultPair9,}),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        (object search, object result) searchResultPair9,
        (object search, object result) searchResultPair10,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,
                searchResultPair9,
                searchResultPair10,}),
            Resolve(@default));

    public static IDeleteBuilderDelete DeleteFrom(DbTableBase table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DenseRank() => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DistinctKeyword Distinct => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();
}
