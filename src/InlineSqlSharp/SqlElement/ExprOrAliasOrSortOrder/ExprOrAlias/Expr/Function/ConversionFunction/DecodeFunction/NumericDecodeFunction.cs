using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericDecodeFunction<TDefault>(
    object expr,
    (object, object)[] searchResultPairs,
    NumericExpr @default) : NumericExpr
    where TDefault : INumber<TDefault>
{
    private readonly DecodeFunctionCore<NumericExpr> _core =
        new(expr, searchResultPairs, @default);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
