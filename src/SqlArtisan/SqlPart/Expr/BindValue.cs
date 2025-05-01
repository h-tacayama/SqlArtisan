using System.Data;

namespace SqlArtisan;

public sealed class BindValue(
    object value,
    DbType? dbType = null,
    ParameterDirection? direction = null,
    int? size = null) : AbstractExpr
{
    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public int? Size => size;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
