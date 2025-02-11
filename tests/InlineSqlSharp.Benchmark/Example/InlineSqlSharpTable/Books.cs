namespace InlineSqlSharp.Benchmark.InlineSqlSharpTable;

internal sealed class Books : Table
{
	public Books(string alias)
		: this(new AliasName(alias))
	{
	}

	private Books(AliasName alias) : base(alias)
	{
		Id = new NumericColumn(alias, "Id");
		Name = new CharacterColumn(alias, "Name");
		AuthorId = new NumericColumn(alias, "AuthorId");
		Rating = new NumericColumn(alias, "Rating");
	}

	public NumericColumn Id { get; }

	public CharacterColumn Name { get; }

	public NumericColumn AuthorId { get; }

	public NumericColumn Rating { get; }
}
