using System.Data;

namespace InlineSqlSharp;

public sealed class DateTimeBindValue(
    DateTime value,
    ParameterDirection direction = ParameterDirection.Input) :
    DateTimeExpr,
    IBindValue
{
    public object Value { get; } = value;

    public DbType DbType { get; } = DbType.DateTime;

    public ParameterDirection Direction { get; } = direction;

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
