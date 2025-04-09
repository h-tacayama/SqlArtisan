namespace InlineSqlSharp;

public sealed class EnumLiteral(Enum value) : NumericExpr, ILiteral
{
    private readonly object _value = value.ToUnderlyingValue();

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_value.ToString());
}
