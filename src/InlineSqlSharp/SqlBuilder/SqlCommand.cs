namespace InlineSqlSharp;

public sealed class SqlCommand(
	string statement,
	IReadOnlyList<BindParameter> parameters)
{
	public string Statement { get; } = statement;

	public IReadOnlyList<BindParameter> Parameters { get; } = parameters;
}
