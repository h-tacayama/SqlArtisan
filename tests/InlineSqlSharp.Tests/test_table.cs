namespace InlineSqlSharp.Tests;

internal sealed class test_table : Table
{
	public test_table(string alias)
		: this(new AliasName(alias))
	{
	}

	private test_table(AliasName alias) : base(alias)
	{
		code = new NumericColumn(alias, "code");
		name = new CharacterColumn(alias, "name");
		created_at = new(alias, "created_at");
	}

	public NumericColumn code { get; }

	public CharacterColumn name { get; }

	public DateTimeColumn created_at { get; }
}
