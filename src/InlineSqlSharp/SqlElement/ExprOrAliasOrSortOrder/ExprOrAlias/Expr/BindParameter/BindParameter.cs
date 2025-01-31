using System.Data;

namespace InlineSqlSharp;

public sealed class BindParameter(
	string name,
	IBoundValue value,
	ParameterDirection direction = ParameterDirection.Input) : IExpr
{
	internal string Name { get; } = name.StartsWith(':') ? name : $":{name}";

	internal IBoundValue Value { get; } = value;

	internal ParameterDirection Direction { get; } = direction;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.BindValue(Value);

	public void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);
}
