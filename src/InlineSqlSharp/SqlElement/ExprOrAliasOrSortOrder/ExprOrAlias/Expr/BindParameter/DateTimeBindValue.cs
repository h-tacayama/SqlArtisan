using System.Data;

namespace InlineSqlSharp;

public sealed class DateTimeBindValue(
	ParameterDirection direction,
	DateTime value) : DateTimeExpr, IBindValue
{
	public DateTimeBindValue(DateTime value)
		: this(ParameterDirection.Input, value) { }

	public DbType DbType { get; } = DbType.DateTime;

	public ParameterDirection Direction { get; } = direction;

	public object Value { get; } = value;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.AddParameter(this);
}
