using System.Data;
using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericBindValue<TValue>(
    TValue value,
    DbType? dbType = null,
    ParameterDirection? direction = null) :
    NumericExpr,
    IBindValue
    where TValue : INumber<TValue>
{
    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
