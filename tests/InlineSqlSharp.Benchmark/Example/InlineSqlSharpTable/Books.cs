namespace InlineSqlSharp.Benchmark.InlineSqlSharpTable;

internal sealed class Books : AbstractTable
{
    public Books(string alias) : base(alias)
    {
        Id = new Column(alias, "Id");
        Name = new Column(alias, "Name");
        AuthorId = new Column(alias, "AuthorId");
        Rating = new Column(alias, "Rating");
    }

    public Column Id { get; }

    public Column Name { get; }

    public Column AuthorId { get; }

    public Column Rating { get; }
}
