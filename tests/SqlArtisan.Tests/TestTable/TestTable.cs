namespace SqlArtisan.Tests;

internal sealed class TestTable : DbTableBase
{
    public TestTable(string alias) : base("test_table", alias)
    {
        Code = new DbColumn(alias, "code");
        Name = new DbColumn(alias, "name");
        CreatedAt = new DbColumn(alias, "created_at");
    }

    public DbColumn Code { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
