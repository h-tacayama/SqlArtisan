using System.Data;
using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericBindValue<TValue> : NumericExpr, IBindValue
    where TValue : INumber<TValue>
{
    public NumericBindValue(
        TValue value,
        ParameterDirection direction = ParameterDirection.Input)
    {
        Value = value;

        DbType = value switch
        {
            sbyte => DbType.SByte,
            byte => DbType.Byte,
            short => DbType.Int16,
            ushort => DbType.UInt16,
            int => DbType.Int32,
            uint => DbType.UInt32,
            long => DbType.Int64,
            ulong => DbType.UInt64,
            float => DbType.Single,
            double => DbType.Double,
            _ => DbType.Decimal,
        };

        Direction = direction;
    }

    public object Value { get; }

    public DbType DbType { get; }

    public ParameterDirection Direction { get; }

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
