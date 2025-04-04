namespace InlineSqlSharp.Tests;

internal sealed class test_table : AbstractTable
{
	public test_table(string alias) : base(alias)
	{
		code = new NumericColumn(alias, "code");
		name = new CharacterColumn(alias, "name");
		created_at = new(alias, "created_at");
	}

	public NumericColumn code { get; }

	public CharacterColumn name { get; }

	public DateTimeColumn created_at { get; }
}
