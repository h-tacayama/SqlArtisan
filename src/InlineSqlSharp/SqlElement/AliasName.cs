namespace InlineSqlSharp;

public sealed class AliasName(string name)
{
	public string Name { get; } = ArgumentValidator.NotEmpty(name);

	public override string ToString() => Name;
}
