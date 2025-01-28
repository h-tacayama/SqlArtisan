namespace InlineSqlSharp;

public sealed class SqlCommand(string statement)
{
	public string Statement { get; } = statement;
}
