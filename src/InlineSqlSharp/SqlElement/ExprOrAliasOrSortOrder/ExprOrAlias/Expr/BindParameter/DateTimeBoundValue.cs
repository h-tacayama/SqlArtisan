using System.Data;

namespace InlineSqlSharp;

internal sealed class DateTimeBoundValue(DateTime value) : DateTimeExpr, IBoundValue
{
	public object Value { get; } = value;

	public DbType DbType { get; } = DbType.DateTime;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.BindValue(this);
}
