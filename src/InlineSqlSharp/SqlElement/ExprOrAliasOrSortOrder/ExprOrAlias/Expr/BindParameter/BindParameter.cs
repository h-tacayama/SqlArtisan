using System.Data;

namespace InlineSqlSharp;

public sealed class BindParameter(string name, IBindValue bindValue)
{
	public string Name { get; } = name.StartsWith(':') ? name : $":{name}";

	public object Value { get; } = bindValue.Value;

	public DbType DbType { get; } = bindValue.DbType;

	public ParameterDirection Direction { get; } = bindValue.Direction;
}
