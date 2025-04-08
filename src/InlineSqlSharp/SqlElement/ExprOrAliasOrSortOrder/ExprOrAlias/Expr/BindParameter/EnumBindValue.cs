using System.Data;

namespace InlineSqlSharp;

public sealed class EnumBindValue(
	Enum value,
	ParameterDirection direction = ParameterDirection.Input)
	: NumericExpr, IBindValue
{
	public object Value { get; } = value.ToUnderlyingValue();

	public DbType DbType { get; } = DbType.Decimal;

	public ParameterDirection Direction { get; } = direction;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.AddParameter(this);
}
