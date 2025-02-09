using System.Data;

namespace InlineSqlSharp;

public sealed class CharacterBindValue(
	ParameterDirection direction,
	string value) : CharacterExpr, IBindValue
{
	public CharacterBindValue(string value)
		: this(ParameterDirection.Input, value) { }

	public DbType DbType { get; } = DbType.String;

	public ParameterDirection Direction { get; } = direction;

	public object Value { get; } = value;

	public override void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AddParameter(this);
}
