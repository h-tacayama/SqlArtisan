using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericDecodeFunction<TDefault> : NumericExpr
    where TDefault : INumber<TDefault>
{
    private readonly DecodeFunctionCore _core;

    public NumericDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        NumericExpr @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public NumericDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        INumber<TDefault> @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
