using System.Data;
using System.Numerics;

namespace InlineSqlSharp;

public sealed class NumericBindValue<TValue>(
	ParameterDirection direction,
	TValue value) : NumericExpr, IBindValue
	where TValue : INumber<TValue>
{
	public NumericBindValue(TValue value)
		: this(ParameterDirection.Input, value) { }

	public DbType DbType { get; } = DbType.Decimal;

	public ParameterDirection Direction { get; } = direction;

	public object Value { get; } = value;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.AddParameter(this);
}
