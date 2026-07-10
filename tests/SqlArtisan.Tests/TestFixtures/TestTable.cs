namespace SqlArtisan.Tests;

internal sealed class TestTable : DbTableBase
{
    public TestTable(string alias = "") : base("test_table", alias)
    {
        Code = new DbColumn(this, "code");
        Name = new DbColumn(this, "name");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn Code { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
