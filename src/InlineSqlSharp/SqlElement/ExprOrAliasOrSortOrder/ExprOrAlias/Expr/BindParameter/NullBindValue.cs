using System.Data;

namespace InlineSqlSharp;

internal sealed class NullBindValue(ParameterDirection direction)
	: DateTimeExpr, IBindValue
{
	public NullBindValue()
		: this(ParameterDirection.Input) { }

	public DbType DbType { get; } = DbType.String;

	public ParameterDirection Direction { get; } = direction;

	public object Value { get; } = DBNull.Value;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AddParameter(this);
}
