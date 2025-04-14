using System.Data;

namespace InlineSqlSharp;

public sealed class EnumBindValue(
    Enum value,
    DbType? dbType = null,
    ParameterDirection? direction = null) :
    NumericExpr,
    IBindValue
{
    public object Value => value.ToUnderlyingValue();

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
