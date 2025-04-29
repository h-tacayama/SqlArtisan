using System.Data;

namespace InlineSqlSharp;

internal sealed class BindValue(
    object value,
    DbType? dbType = null,
    ParameterDirection? direction = null) : AbstractExpr
{
    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
