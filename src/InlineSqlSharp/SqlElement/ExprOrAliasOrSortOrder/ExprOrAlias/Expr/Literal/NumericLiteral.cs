using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericLiteral<TValue>(TValue value) : NumericExpr, ILiteral
    where TValue : INumber<TValue>
{
    private readonly TValue _value = value;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_value.ToString());
}
