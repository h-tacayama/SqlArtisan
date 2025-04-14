using System.Data;

namespace InlineSqlSharp;

public sealed class DateTimeBindValue(
    DateTime value,
    DbType? dbType = null,
    ParameterDirection? direction = null) :
    DateTimeExpr,
    IBindValue
{
    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
