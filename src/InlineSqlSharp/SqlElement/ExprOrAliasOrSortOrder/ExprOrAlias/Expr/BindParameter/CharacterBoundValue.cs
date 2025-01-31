using System.Data;

namespace InlineSqlSharp;

internal sealed class CharacterBoundValue(string value) : CharacterExpr, IBoundValue
{
	public object Value { get; } = value;

	public DbType DbType { get; } = DbType.String;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.BindValue(this);
}
