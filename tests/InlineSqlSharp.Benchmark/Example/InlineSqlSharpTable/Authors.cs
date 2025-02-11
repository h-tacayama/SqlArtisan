namespace InlineSqlSharp.Benchmark.InlineSqlSharpTable;

internal sealed class Authors : Table
{
	public Authors(string alias)
		: this(new AliasName(alias))
	{
	}

	private Authors(AliasName alias) : base(alias)
	{
		Id = new NumericColumn(alias, "Id");
		Name = new CharacterColumn(alias, "Name");
	}

	public NumericColumn Id { get; }

	public CharacterColumn Name { get; }
}
