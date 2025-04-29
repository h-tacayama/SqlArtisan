namespace SqlArtisan.Tests;

internal sealed class TestTable : AbstractTable
{
    public TestTable(string alias) : base("test_table", alias)
    {
        Code = new Column(alias, "code");
        Name = new Column(alias, "name");
        CreatedAt = new Column(alias, "created_at");
    }

    public Column Code { get; }

    public Column Name { get; }

    public Column CreatedAt { get; }
}
