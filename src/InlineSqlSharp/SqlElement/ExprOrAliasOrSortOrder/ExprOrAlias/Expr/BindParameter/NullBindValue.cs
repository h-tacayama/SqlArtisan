using System.Data;

namespace InlineSqlSharp;

internal sealed class NullBindValue(
    ParameterDirection direction = ParameterDirection.Input) :
    DateTimeExpr,
    IBindValue
{
    public object Value { get; } = DBNull.Value;

    public DbType DbType { get; } = DbType.String;

    public ParameterDirection Direction { get; } = direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
