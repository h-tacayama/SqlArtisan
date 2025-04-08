using System.Data;

namespace InlineSqlSharp;

public sealed class CharacterBindValue(
	string value,
	ParameterDirection direction = ParameterDirection.Input)
	: CharacterExpr, IBindValue
{
	public CharacterBindValue(
		char value,
		ParameterDirection direction = ParameterDirection.Input)
		: this(value.ToString(), direction) { }

	public object Value { get; } = value;

	public DbType DbType { get; } = DbType.String;

	public ParameterDirection Direction { get; } = direction;

	public override void FormatSql(SqlBuildingBuffer buffer) =>
		buffer.AddParameter(this);
}
