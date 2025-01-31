using System.Data;

namespace InlineSqlSharp;

internal sealed class NullBoundValue : DateTimeExpr, IBoundValue
{
	private NullBoundValue()
	{
		Value = DBNull.Value;
	}

	public object Value { get; }

	public DbType DbType { get; } = DbType.String;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.BindValue(this);
}
