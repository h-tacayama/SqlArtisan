namespace SqlArtisan.Tests;

internal sealed class TestDerivedTable : DerivedTableSchemaBase
{
    public TestDerivedTable(string name) : base(name)
    {
        Code = new DbColumn(name, "code");
        Name = new DbColumn(name, "name");
        CreatedAt = new DbColumn(name, "created_at");
    }

    public DbColumn Code { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
