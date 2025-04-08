using System.Data;

namespace InlineSqlSharp;

public sealed class EnumBindValue(
	ParameterDirection direction,
	Enum value) : NumericExpr, IBindValue
{
	public EnumBindValue(Enum value)
		: this(ParameterDirection.Input, value) { }

	public DbType DbType { get; } = DbType.Decimal;

	public ParameterDirection Direction { get; } = direction;

	public object Value { get; } = value.ToUnderlyingValue();

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.AddParameter(this);
}
