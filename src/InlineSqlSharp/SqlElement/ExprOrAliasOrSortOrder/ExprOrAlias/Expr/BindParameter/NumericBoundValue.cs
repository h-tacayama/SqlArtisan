using System.Data;
using System.Numerics;

namespace InlineSqlSharp;

internal sealed class NumericBoundValue<TValue>(TValue value) : NumericExpr, IBoundValue
	where TValue : INumber<TValue>
{
	public object Value { get; } = value;

	public DbType DbType { get; } = DbType.Decimal;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.BindValue(this);
}
